﻿namespace IronJS.Runtime.Helpers

open IronJS
open IronJS.Aliases
open IronJS.Tools
open IronJS.Runtime

module Variables = 
  type private ObjectList = Object ResizeArray

  let rec private scanScopes fnc (lst:Scope ResizeArray) topScope = 
    let rec scanScopes n = 
      if n >= lst.Count
        then  false, null
        else  let scope = lst.[n]
              if scope.ScopeLevel < topScope 
                then  false, null
                else  let pair = fnc scope
                      if fst pair
                        then pair
                        else scanScopes (n+1)
    scanScopes 0

  let private setInObjects (name:string) (value:Dynamic) scopes = 
    if scopes = null
      then  false
      else  match ResizeArray.tryFind (fun (s:Object) -> s.Has name) scopes with
            | None    -> false
            | Some(s) -> s.Set name value; true

  let private getFromObjects (name:string) scopes =
    if scopes = null
      then  false, null
      else  match ResizeArray.tryFind (fun (s:Object) -> s.Has name) scopes with
            | None    -> false, null
            | Some(s) -> true, s.Get name

  (**)
  type Locals = 
    static member Get(name:string, localScopes:ObjectList) =
      let pair = getFromObjects name localScopes
      if (fst pair)
        then  pair
        else  false, null

    static member Set(name:string, value:Dynamic, localScopes:ObjectList) =
      setInObjects name value localScopes

  (**)
  type Closures =
    static member Get(name:string, localScopes:ObjectList, closure:Closure, maxScopeLevel:int) =
      let pair = getFromObjects name localScopes
      if (fst pair)
        then  pair
        else  let pair = scanScopes (fun (x:Scope) -> getFromObjects name x.Objects) closure.Scopes maxScopeLevel
              if (fst pair)
                then pair
                else false, null

    static member Set(name:string, value:Dynamic, localScopes:ObjectList, closure:Closure, maxScopeLevel:int) = 
      if setInObjects name value localScopes
        then  true
        else  let found, _ = scanScopes (fun (x:Scope) -> setInObjects name value x.Objects, null) closure.Scopes maxScopeLevel
              found

  (**)
  type Globals =
    static member Get(name:string, localScopes:ObjectList, closure:Closure) = 
      let found, item = getFromObjects name localScopes
      if found 
        then  item
        else  let found, item = scanScopes (fun (x:Scope) -> getFromObjects name x.Objects) closure.Scopes (-1)
              if found 
                then item
                else closure.Globals.Get name
  
    static member Set(name:string, value:Dynamic, localScopes:ObjectList, closure:Closure) = 
      if not (setInObjects name value localScopes) 
        then if not (ResizeArray.exists (fun (x:Scope) -> setInObjects name value x.Objects) closure.Scopes)
             then closure.Globals.Set name value
      value
  