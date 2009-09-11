#light

namespace Yadic

open System;;
open System.Linq;;
open System.Reflection;;
open System.Collections.Generic;;

type public ContainerException(s) =
 inherit Exception(s)
 
type IContainer = 
 abstract Add<'T> : unit -> unit
 [<OverloadID("addByInterface")>]
 abstract Add<'Interface, 'Component> : unit -> unit
 [<OverloadID("addByConcrete")>]
 abstract Add<'T> : Func<obj> -> unit
 abstract Resolve<'T> : unit -> 'T
 abstract Resolve : Type -> obj
 abstract Decorate<'Interface, 'Component> : unit -> unit

type SimpleContainer(missingHandler:Func<Type,obj>) = 
 let activators = new Dictionary<Type, unit->obj>()
 
 let get t = 
  match activators.ContainsKey(t) with 
  | true ->  
   let instance = activators.[t]()
   activators.[t] <- fun() -> instance
   instance
  | false -> t |> missingHandler.Invoke
  
 let toInstance (t:Type, replacement) (p:ParameterInfo) = 
  let shouldReplace = p.ParameterType.Equals(t)
  match shouldReplace,replacement with
  | true,Some(o) -> o
  | _ -> p.ParameterType |> get
  
 let getParameters (t:Type) = 
  let sortedConstructors = t.GetConstructors() |> Seq.sortBy (fun c->c.GetParameters().Count())
  sortedConstructors.LastOrDefault().GetParameters()
 
 let getArgs (t,replacement) = 
  getParameters >> Array.map ((t,replacement) |> toInstance)
 
 let createInstance = Activator.CreateInstance : Type * obj array -> obj 
 
 let create (i,c,existing)  = 
  ( c, c |> getArgs (i,existing) ) |> createInstance
 
 let addActivator (t,f) =
  match activators.ContainsKey(t) with
  | true -> raise( new ContainerException(t.ToString() + " already added to container"))
  | false -> activators.Add(t,f)
 
 let add (i,c) =
  addActivator (i, fun() -> create (i,c,None))
  
 let decorate (i,c) =
  let existing = get i
  let result = i |> activators.Remove
  addActivator (i, fun() -> create (i,c,Some(existing)) )
 
 new() = SimpleContainer(fun (t) -> raise ( new ContainerException(t.ToString() + " not found in container") ))
 
 interface IContainer with
  member this.Add<'T>() = (typeof<'T>,typeof<'T>) |> add
  
  [<OverloadID("addByInterface")>]
  member this.Add<'Interface,'Component>() = (typeof<'Interface>,typeof<'Component>) |> add 
  
  [<OverloadID("addByConcrete")>]
  member this.Add<'T>(f) = (typeof<'T>, fun () -> f.Invoke() ) |> addActivator
  
  member this.Resolve<'T>() = unbox<'T>( typeof<'T> |> get )
  member this.Resolve(t) = t |> get
  member this.Decorate<'Interface,'Decorator>() = (typeof<'Interface>,typeof<'Decorator>) |> decorate
  


 


