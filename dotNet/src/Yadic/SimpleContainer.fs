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

type IActivator =
 abstract Activate: unit -> obj

type LazyActivator(activator:unit->obj) =
 let instance = lazy( activator() )

 interface IActivator with
  member this.Activate() = instance.Force() 


type SimpleContainer(missingHandler:Func<Type,obj>) = 
 let activators = new Dictionary<Type, IActivator>()
 
 let resolve t = 
  match activators.ContainsKey(t) with 
  | true -> activators.[t].Activate()
  | false -> t |> missingHandler.Invoke
  
 let createInstance (t:Type, resolver:(Type -> obj) ) = 
  let sortedConstructors = t.GetConstructors() |> Array.sortBy(fun c -> -c.GetParameters().Length)
  sortedConstructors |> Array.pick( fun c ->
    try 
      let instances = c.GetParameters() |> Array.map( fun p -> resolver(p.ParameterType) )
      Some(Activator.CreateInstance(t, instances))
    with | :? ContainerException -> None )
 
 let create (t:Type) = createInstance(t, resolve) 

 let addActivator (t,f) =
  match activators.ContainsKey(t) with
  | true -> raise( new ContainerException(t.ToString() + " already added to container"))
  | false -> activators.Add(t, new LazyActivator(f))
 
 let add (i,c) =
  addActivator (i, fun() -> create(c) )
  
 let decorate (i,c) =
  let existing = activators.[i]
  activators.[i] <- new LazyActivator(fun() -> 
   createInstance(c, fun t -> 
    if t.Equals(i) then existing.Activate() else resolve(t)))

 new() = SimpleContainer(fun (t) -> raise ( new ContainerException(t.ToString() + " not found in container") ))
 
 interface IContainer with
  member this.Add<'T>() = (typeof<'T>,typeof<'T>) |> add
  
  [<OverloadID("addByInterface")>]
  member this.Add<'Interface,'Component>() = (typeof<'Interface>,typeof<'Component>) |> add 
  
  [<OverloadID("addByConcrete")>]
  member this.Add<'T>(f) = (typeof<'T>, fun () -> f.Invoke() ) |> addActivator
  
  member this.Resolve<'T>() = typeof<'T> |> resolve :?> 'T 
  member this.Resolve(t) = t |> resolve
  member this.Decorate<'Interface,'Decorator>() = (typeof<'Interface>,typeof<'Decorator>) |> decorate
  
