package com.googlecode.yadic

class LazyInstanceActivator(creator:() => Object) extends Activator{
  lazy val instance = creator()
  def activate = instance
}