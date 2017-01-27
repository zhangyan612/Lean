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

using System;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
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
            object result;
            var success = true;
            try
            {
                result = Evaluate(Query, algorithm);
            }
            catch (Exception err)
            {
                Log.Error(err.Message);
                result = err;
                success = false;
            }
            return new Result(this, success, result);
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
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public object Evaluate(string code, IAlgorithm algorithm)
        {
            var c = new CSharpCodeProvider();
            var icc = c.CreateCompiler();
            var cp = new CompilerParameters();


            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("mscorlib.dll");
            if (OS.IsWindows)
            {
                cp.ReferencedAssemblies.Add("System.Linq.dll");
            }
            else
            {
                cp.ReferencedAssemblies.Add("System.Core.dll");
            }
            cp.ReferencedAssemblies.Add("QuantConnect.Common.dll");
            cp.ReferencedAssemblies.Add("QuantConnect.Algorithm.dll");
            cp.ReferencedAssemblies.Add("QuantConnect.Indicators.dll");

            cp.CompilerOptions = "/t:library";
            cp.GenerateInMemory = true;
            var sb = new StringBuilder("");
            foreach (var statement in UsingStatements)
            {
                sb.Append(statement);
            }

            sb.Append("namespace QuantConnect { \n");
            sb.Append("public class Inspector { \n");
            sb.Append("public object EvaluateCode(IAlgorithm algorithm) {\n");
            sb.Append("return " + code + "; \n");
            sb.Append("} \n");
            sb.Append("} \n");
            sb.Append("}\n");

            var cr = icc.CompileAssemblyFromSource(cp, sb.ToString());
            if (cr.Errors.HasErrors)
            {
                Log.Error("InspectorCommand(): Error evaluating code: " + cr.Errors[0].ErrorText);
                return null;
            }

            var a = cr.CompiledAssembly;
            var o = a.CreateInstance("QuantConnect.Inspector");
            var t = o.GetType();
            var mi = t.GetMethod("EvaluateCode");
            var oParams = new object[1];
            oParams[0] = algorithm;
            var s = mi.Invoke(o, oParams);
            return s;
        }

        private string[] UsingStatements
        {
            get
            {
                return new[] {
                        "using System;",
                        "using System.Collections;",
                        "using System.Collections.Generic;",
                        "using System.Linq;",
                        "using System.Globalization;",
                        "using QuantConnect;",
                        "using QuantConnect.Parameters;",
                        "using QuantConnect.Benchmarks;",
                        "using QuantConnect.Brokerages;",
                        "using QuantConnect.Util;",
                        "using QuantConnect.Interfaces;",
                        "using QuantConnect.Algorithm;",
                        "using QuantConnect.Indicators;",
                        "using QuantConnect.Data;",
                        "using QuantConnect.Data.Consolidators;",
                        "using QuantConnect.Data.Custom;",
                        "using QuantConnect.Data.Fundamental;",
                        "using QuantConnect.Data.Market;",
                        "using QuantConnect.Data.UniverseSelection;",
                        "using QuantConnect.Notifications;",
                        "using QuantConnect.Orders;",
                        "using QuantConnect.Orders.Fees;",
                        "using QuantConnect.Orders.Fills;",
                        "using QuantConnect.Orders.Slippage;",
                        "using QuantConnect.Scheduling;",
                        "using QuantConnect.Securities;",
                        "using QuantConnect.Securities.Equity;",
                        "using QuantConnect.Securities.Forex;",
                        "using QuantConnect.Securities.Interfaces;",
                    };
            }
        }
    }
}