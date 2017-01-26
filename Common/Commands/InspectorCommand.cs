/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/
using System.CodeDom.Compiler;
using System.Text;
using Microsoft.CSharp;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.Commands
{
    /// <summary>
    /// Inspector query console tool.
    /// </summary>
    public class InspectorCommand : ICommand
    {
        /// <summary>
        /// Query to execute on the algorithm
        /// </summary>
        public string Query;
        
        /// <summary>
        /// Execute the generic query on the algorithm
        /// </summary>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public CommandResultPacket Run(IAlgorithm algorithm)
        {
            var result = Evaluate(Query);
            return new Result(this, true, result);
        }

        /// <summary>
        /// Return the evaluated result back to the algorithm
        /// </summary>
        public class Result : CommandResultPacket
        {
            /// <summary>
            /// Resulting object returned from the algorithm for display
            /// </summary>
            public object EvaluatedResult;

            /// <summary>
            /// Evaluated result
            /// </summary>
            /// <param name="command"></param>
            /// <param name="success"></param>
            /// <param name="result"></param>
            public Result(ICommand command, bool success, object result) : base(command, success)
            {
                EvaluatedResult = result;
            }
        }

        /// <summary>
        /// Evaluate the code provided
        /// </summary> 
        /// <param name="code"></param>
        /// <returns></returns>
        private object Evaluate(string code)
        {
            var c = new CSharpCodeProvider();
            var icc = c.CreateCompiler();
            var cp = new CompilerParameters();

            cp.ReferencedAssemblies.Add("system.dll");
            cp.ReferencedAssemblies.Add("system.xml.dll");
            cp.ReferencedAssemblies.Add("system.data.dll");
            cp.CompilerOptions = "/t:library";
            cp.GenerateInMemory = true;
            var sb = new StringBuilder("");

            sb.Append("using System;\n");
            sb.Append("using System.Xml;\n");
            sb.Append("using System.Data;\n");

            sb.Append("namespace CSCodeEvaler{ \n");
            sb.Append("public class CSCodeEvaler{ \n");
            sb.Append("public object EvalCode(){\n");
            sb.Append("return " + code + "; \n");
            sb.Append("} \n");
            sb.Append("} \n");
            sb.Append("}\n");

            var cr = icc.CompileAssemblyFromSource(cp, sb.ToString());
            if (cr.Errors.Count > 0)
            {
                return null;
            }

            var a = cr.CompiledAssembly;
            var o = a.CreateInstance("CSCodeEvaler.CSCodeEvaler");
            var t = o.GetType();
            var mi = t.GetMethod("EvalCode");
            var s = mi.Invoke(o, null);
            return s;
        }
    }
}