﻿/* *****************************************************************************************************************************
 * (c) J@mBeL.net 2010-2017
 * Author: John Ambeliotis
 * Created: 24 Apr. 2010
 *
 * License:
 *  This file is part of jaNET Framework.

    jaNET Framework is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    jaNET Framework is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with jaNET Framework. If not, see <http://www.gnu.org/licenses/>. */

using jaNET.Environment;
using jaNET.Environment.Core;
using jaNET.Net.Http;
using System;
//using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace jaNET.Providers
{
    class Evaluator
    {
        Evaluator(EvaluatorItem[] items) {
            ConstructEvaluator(items);
        }

        Evaluator(Type returnType, string expression, string name) {
            EvaluatorItem[] items = { new EvaluatorItem(returnType, expression, name) };
            ConstructEvaluator(items);
        }

        Evaluator(EvaluatorItem item) {
            EvaluatorItem[] items = { item };
            ConstructEvaluator(items);
        }

        void ConstructEvaluator(EvaluatorItem[] items) {
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
            var comp = new CompilerParameters();
            //comp.ReferencedAssemblies.Add("system.dll");
            //comp.GenerateExecutable = false;
            //comp.GenerateInMemory = true;

            var code = new StringBuilder();
            code.Append("using System; \n");
            code.Append("namespace libJanet { \n");
            code.Append("public class _Evaluator { \n");

            foreach (EvaluatorItem item in items) {
                code.AppendFormat(" public {0} {1}() ", item.ReturnType.Name, item.Name);
                code.Append(" { ");
                code.AppendFormat(" return ({0}); ", item.Expression);
                code.Append(" }\n");
            }
            code.Append("} }");

            CompilerResults cr = provider.CompileAssemblyFromSource(comp, code.ToString());
            if (cr.Errors.HasErrors) {
                var error = new StringBuilder();
                error.Append("Error Compiling Expression: ");
                foreach (CompilerError err in cr.Errors) {
                    error.AppendFormat("{0}\n", err.ErrorText);
                }
                throw new Exception("Error Compiling Expression: " + error);
            }
            Assembly a = cr.CompiledAssembly;
            _Compiled = a.CreateInstance("libJanet._Evaluator");
        }

        #region Private Members
        int EvaluateInt(string name) {
            return (int)Evaluate(name);
        }

        string EvaluateString(string name) {
            return (string)Evaluate(name);
        }

        bool EvaluateBool(string name) {
            return (bool)Evaluate(name);
        }

        object Evaluate(string name) {
            MethodInfo mi = _Compiled.GetType().GetMethod(name);
            return mi.Invoke(_Compiled, null);
        }
        #endregion

        #region Static Members
        static internal string EvaluateCondition(string sValue) {
            //sValue = sValue.Replace("\r", " ").Replace("\n", " ");

            var mItems = Regex.Matches(sValue, @"\{(.*?)\}");

            foreach (Match matchString in mItems) {
                if (matchString.Success) {
                    string[] args = matchString.Value.Split(';');
                    if (args[0].Contains("*"))
                        args[0] = args[0].ParseTokens();
                    if (args[0].Contains("evalBool")) {
                        string condition = Regex.Replace(args[0], "evalBool|[{}]|[()]", string.Empty).Trim();

                        bool e;

                        if (condition.Contains("~>")) {
                            string[] vals = Regex.Split(condition, "~>");
                            e = vals[0].Replace("\"", string.Empty).Contains(vals[1].Replace("\"", string.Empty));
                        }
                        else
                            e = EvaluateToBool(condition);

                        sValue = e ? sValue.Replace(matchString.Value, Parser.Instance.Parse(args[1].Trim().Replace(" ", ";"), WebServer.Request.DataType.text, true)) :
                                     sValue.Replace(matchString.Value, Parser.Instance.Parse(args[2].Trim().Replace(" ", ";"), WebServer.Request.DataType.text, true));
                    }
                }
            }
            return sValue;
        }

        static internal int EvaluateToInteger(string code) {
            var eval = new Evaluator(typeof(int), code, staticMethodName);
            return (int)eval.Evaluate(staticMethodName);
        }

        static internal string EvaluateToString(string code) {
            var eval = new Evaluator(typeof(string), code, staticMethodName);
            return (string)eval.Evaluate(staticMethodName);
        }

        static internal bool EvaluateToBool(string code) {
            var eval = new Evaluator(typeof(bool), code, staticMethodName);
            return (bool)eval.Evaluate(staticMethodName);
        }

        static internal object EvaluateToObject(string code) {
            var eval = new Evaluator(typeof(object), code, staticMethodName);
            return eval.Evaluate(staticMethodName);
        }
        #endregion

        #region Private
        const string staticMethodName = "__foo";
        //Type _CompiledType = null;
        object _Compiled; // = null;
        #endregion
    }

    class EvaluatorItem
    {
        internal EvaluatorItem(Type returnType, string expression, string name) {
            ReturnType = returnType;
            Expression = expression;
            Name = name;
        }

        internal Type ReturnType;
        internal string Name;
        internal string Expression;
    }
}