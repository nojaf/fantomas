# Daemon mode

## Introduction

Fantomas can be a prominent part of the F# developer experience.
While writing code, it can be using before, during and after.
To facilitate this, integration inside an IDE is highly desirable.

In previous integrations with IDE the Fantomas core library was referenced used accordingly. 
The most troublesome restriction there was that the version of the F# compiler the tooling uses needs to match the version Fantomas is using.
Fantomas could then re-use the F# AST nodes and print out the formatted code.
This in theory was optimal in terms of performance, but adds a huge burden in terms for maintainability.
In practise, Fantomas quite often needed to construct the AST nodes anyways and the benefit of sharing the `FSharpChecker` was never really confirmed.

Updating Fantomas required careful coordination of the F# compiler version and was not always trivial.
Fantomas itself keeps evolving, over time more bugs get fixed and the two styles guides are more accurately worked out.  
This creates friction among the end-users, for example, a bug is fixed but the only to use a version of Fantomas with a fix is via the dotnet tool.

To solve the challenges above, a new mechanism of integrating with editor tooling was figured out.

## Enter the daemon

"What if the user could bring their own version of Fantomas to their IDE?"  
This would allow users to move to newer version of Fantomas at their own pace and
it allows IDE tooling and CI checks to run on the same version with ease.

### Out of process

The idea of "out-of-process" formatting resolves the restriction of using the same compiler version.
Fantomas generally benefits from newer version of the [FCS](https://www.nuget.org/packages/FSharp.Compiler.Service) package.  

This could be achieved by running the current console application in a separate process.
Yet, this has some drawbacks as the console application will interact with the file system and will create and dispose of the FSharpChecker after each run.  
To get around this limitation, a new "mode" of starting the console application would be introduce.

> dotnet fantomas --daemon

### Daemon mode

Running in `daemon` mode would start a Fantomas [LSP](https://microsoft.github.io/language-server-protocol/) language server.  
This would keep the process running and can interact with the IDE tooling close to the metal without using the file system.

LSP would mainly be used for its protocol rather than trying to be a correct implementation of the LSP spec.
Fantomas LSP would expose some custom endpoints to format a full document, a selection and expose the current available settings.
Editor tooling could uses these endpoints to format at will and provide feedback regarding the current `.editorconfig`.

### LSP endpoints

TODO

#### fantomas/formatDocument

#### fantomas/formatSelection

#### fantomas/configuration

#### fantomas/version

