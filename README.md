# WasmNet
An experimental WebAssembly runtime for .NET. 
This project provides a .wasm binary reader, WebAssembly-to-CIL cross-compiler, and runtime with which to execute WebAssembly code.
The WebAssembly instructions are first compiled to [CIL](https://learn.microsoft.com/en-us/dotnet/standard/managed-code) instructions 
via [IL Emit](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.emit?view=net-7.0) before being invoked as ordinary 
in-memory .NET functions.

This is a learning and research project for me, so it is fairly incomplete and experimental.
It is completely unsuitable at the moment for production code, and may have security concerns.

This project is not accepting Pull Requests at this time, although issues are welcome.
Due to the research and learning nature of this project, and to help mitigate its use in production currently 
(as your employer's lawyers likely would not be happy about this), it is AGPLv3 licensed.
If this project ever becomes mature, I may consider a more permissive license in the future.

The unit test project has several tests which are being created using Test-Driven Development.
The project currently requires [wat2wasm](https://github.com/WebAssembly/wabt) to be on your PATH to build.
Each test is executed in the following manner:

1. The [WebAssembly Text](https://developer.mozilla.org/en-US/docs/WebAssembly/Understanding_the_text_format) test file is
   converted to a .wasm binary via wat2wasm at build time.
2. Comments in the header of the .wat file are parsed for i.e. declaring globals, invoking methods, and asserting the return values.
3. Any external globals/importables are initialized into the runtime.
4. The .wasm binary is loaded into the runtime (which also compiles all functions and runs global initializers).
5. The header comment directives are evaluated in order (except for `global` which was done earlier),
   such as invoking functions and asserting their output.

