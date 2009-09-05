package com.googlecode.yadic


trait Container {
  def add(concrete:Class[_]): Unit
  def add(interface:Class[_], concrete:Class[_]): Unit
  def add(aClass:Class[_], activator:() => Object ): Unit
  def decorate(decorator:Class[_], previous:Class[_]): Unit
  def resolve( aClass:Class[_] ): Object
}