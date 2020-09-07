# PInvoke Interop Assistant

In marshalling there are many attributes and rules for dealing with types. Understanding all of these can be a bit daunting. To improve the development experience, the P/Invoke Interop Assistant was created. It is a tool that conveniently converts C/C++ to managed P/Invoke signatures or verse visa. It even comes with a database of Win32 functions, data types, and constants, so the common task of adding a Win32 P/Invoke to your C# source file is made very easy.

This project is a modernization of the [PInvoke Interop Assistant Toolkit](http://clrinterop.codeplex.com/releases/view/14120) which I started almost a decade ago.  The intent is to modernize and refactor the code such that it can be consumed as a library and potentially distributed via new channels.  For example distribute as a modern VSIX or MSBuild plugin.

This is a hobby project and could definitely use a bit of community participation.  Issues will outline the plan for the code going forward.

AppVeyor [![Build status](https://ci.appveyor.com/api/projects/status/uqe54r8d83kg3l6s?svg=true)](https://ci.appveyor.com/project/jaredpar/pinvoke)

