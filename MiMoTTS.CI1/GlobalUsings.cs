// WPF 的 MarkupCompilePass1 生成的临时工程（*_wpftmp.csproj）不会带上 SDK 生成的
// obj/*.GlobalUsings.g.cs，导致隐式 using 全部失效。这里显式声明一份，保证两处都能编译。
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;
