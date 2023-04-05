using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using JavaToCSharp.Configuration;
using System.Configuration;
using System.Linq;
using JavaToCSharp.Rules;

namespace JavaToCSharp
{
    public class RuleEngine
    {
        private readonly List<Rule> _rules = new();
        private const string NAME_SPACE = "JavaToCSharp.Rules";

        public void AddRule(Rule rule)
        {
            _rules.Add(rule);
        }

        /// <summary>
        /// Loads the rules.
        /// </summary>
        public void LoadRules()
        {
            if (ConfigurationManager.GetSection("J2CS") is J2CSSection config)
            {
                foreach (RuleSection rs in config.Rules)
                {
                    var r = new EquivalentRule(rs.Name)
                    {
                        Pattern = rs.Pattern,
                        Replacement = rs.Replacement
                    };

                    AddRule(r);
                }
            }

            //load from assembly
            var asm = Assembly.GetExecutingAssembly();

            foreach (var type in asm.GetTypes())
            {
                if (type.Namespace == NAME_SPACE && !type.IsAbstract && type.Name != "EquivalentRule")
                    AddRule(Activator.CreateInstance(type) as Rule);
            }
        }

        protected static void Log(string message)
        {
            Console.WriteLine(message);
        }

        protected static void LogExecute(string ruleName,int iRowNumber)
        {
            Log($"[Line {iRowNumber}] {ruleName}");
        }

        public void Run(string path)
        {
            //read from file
            if (!File.Exists(path))
            {
                Console.WriteLine("File not found");
                return;
            }

            using var sr = new StreamReader(path, true);
            var strOrigin = sr.ReadToEnd();
            sr.Close();

            //run rules
            var sb = new StringBuilder();
            var arrInput = strOrigin.Split(new[] {'\n'});
            for (var i = 0; i < arrInput.Length; i++)
            {
                var tmp = arrInput[i].Replace("\r", "");

                var ruleNum = i + 1;
                foreach (var rule in _rules.Where(rule => rule.Execute(tmp, out tmp, ruleNum)))
                {
                    LogExecute(rule.RuleName, ruleNum);
                }

                sb.AppendLine(tmp);
            }

            //save result to file
            using var sw = new StreamWriter(path + ".cs", false);
            sw.Write(sb);
            sw.Close();
        }
    }
}
