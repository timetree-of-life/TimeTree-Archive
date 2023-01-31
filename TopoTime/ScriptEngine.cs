using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace TopoTime
{    public class CSharpScriptEngine
    {
        private static ScriptState scriptState = null;
        public static object Execute(string code)
        {
            scriptState = scriptState == null ? CSharpScript.RunAsync(code).Result : scriptState.ContinueWithAsync(code).Result;
            if (scriptState.ReturnValue != null && !string.IsNullOrEmpty(scriptState.ReturnValue.ToString()))
                return scriptState.ReturnValue;
            return null;
        }
    }
}
