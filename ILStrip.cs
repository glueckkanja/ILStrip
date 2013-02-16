using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ILStrip
{
    class ILStrip
    {
        public string InputFileName { get; set; }
        public string OutputFileName { get; set; }
        public int Verbose { get; set; }

        public string KeepResources { get; set; }
        public string RemoveResources { get; set; }

        List<string> _typeIdsFound = new List<string>();
        int _addScanTypeRecursionLevel = -1;
        Regex _typeIdRootRegEx = new Regex(@".*?`\d+", RegexOptions.Compiled);

        public void Run()
        {
            Console.WriteLine("Opening assembly {0}", InputFileName);

            var readWriteSymbols = File.Exists(Path.ChangeExtension(InputFileName, ".pdb"));

            var assembly = AssemblyDefinition.ReadAssembly(InputFileName, new ReaderParameters { ReadSymbols = readWriteSymbols });
            var mainModule = assembly.MainModule;
            var allTypes = mainModule.Types;
            var allResources = mainModule.Resources;


            _typeIdsFound.Add("<Module>");

            if (assembly.EntryPoint != null)
                AddScanType(assembly.EntryPoint.DeclaringType);

            foreach (var typeDef in allTypes.Where(t => t.IsPublic))
                AddScanType(typeDef);

            Console.WriteLine("Found {0} accessible types.", _typeIdsFound.Count);


            var removeTypeCount = 0;
            var typeIdRoots = _typeIdsFound.Select(t => GetTypeIdRoot(t)).Distinct().ToList();   // reduce names of generic types including parameters to root type name

            foreach (var typeDef in allTypes.Where(t => !typeIdRoots.Contains(t.FullName)).ToList())
            {
                if (Verbose >= 2) Console.WriteLine("Removing: {0}", typeDef);
                allTypes.Remove(typeDef);
                removeTypeCount++;
            }

            Console.WriteLine("Removed {0} inaccessible types.", removeTypeCount);


            IEnumerable<Resource> removeResources = null;
            
            if (!string.IsNullOrEmpty(KeepResources))
            {
                var keep = new List<Resource>();
                foreach (var resRegEx in KeepResources.Split(','))
                {
                    var regEx = new Regex(resRegEx, RegexOptions.Compiled);
                    keep = keep.Concat(mainModule.Resources.Where(r => regEx.IsMatch(r.Name))).ToList();
                }
                removeResources = allResources.Where(r => !keep.Contains(r));
            }
            
            else if (!string.IsNullOrEmpty(RemoveResources))
            {
                foreach (var resRegEx in RemoveResources.Split(','))
                {
                    var regEx = new Regex(resRegEx, RegexOptions.Compiled);
                    removeResources = (removeResources ?? new List<Resource>()).Concat(allResources.Where(r => regEx.IsMatch(r.Name)));
                }
            }

            if (removeResources != null)
            {
                var removeResCount = 0;
                
                foreach (var removeRes in removeResources.ToList())
                {
                    if (Verbose >= 2) Console.WriteLine("Removing: {0}", removeRes);
                    allResources.Remove(removeRes);
                    removeResCount++;
                }
                
                Console.WriteLine("Removed {0} resources.", removeResCount);
            }


            Console.WriteLine("Saving assembly to {0}", OutputFileName);
            assembly.Write(OutputFileName, new WriterParameters { WriteSymbols = readWriteSymbols });

            Console.WriteLine("Done.");

        }

        void AddScanType(TypeReference typeRef)
        {

            if (typeRef == null || _typeIdsFound.Contains(typeRef.FullName))
                return;


            _addScanTypeRecursionLevel++;
            if (Verbose >= 1) Console.WriteLine(string.Format("Processing: {0}{1}", "".PadRight(_addScanTypeRecursionLevel * 2), typeRef.FullName));

            _typeIdsFound.Add(typeRef.FullName);


            if (typeRef is GenericInstanceType)
            {
                foreach (var genericArg in ((GenericInstanceType)typeRef).GenericArguments)
                    AddScanType(genericArg);
            }


            TypeDefinition typeDef;
            if ((typeDef = typeRef as TypeDefinition) != null)
            {
                var methods = typeDef.Methods.ToList();
                var bodies = methods.Where(m => m.HasBody).Select(m => m.Body).ToList();
                var operands = bodies.SelectMany(b => b.Instructions).Select(i => i.Operand).ToList();

                var types = typeDef.Interfaces
                    .Union(typeDef.CustomAttributes.Select(ca => ca.AttributeType))
                    .Union(typeDef.Events.Select(e => e.EventType))
                    .Union(typeDef.Fields.Select(f => f.FieldType))
                    .Union(typeDef.GenericParameters)
                    .Union(methods.SelectMany(m => m.Parameters).Select(p => p.ParameterType))
                    .Union(methods.Select(m => m.ReturnType))
                    .Union(bodies.SelectMany(b => b.Variables).Select(v => v.VariableType))
                    .Union(operands.Where(o => o is TypeReference).Select(o => (TypeReference)o))
                    .Union(operands.Where(o => o is MemberReference).Select(o => ((MemberReference)o).DeclaringType));

                foreach (var t in types)
                    AddScanType(t);
            }


            _addScanTypeRecursionLevel--;
        }

        string GetTypeIdRoot(string input)
        {
            var match = _typeIdRootRegEx.Match(input);
            if (match.Success)
                return match.Value;
            else
                return input;
        }
    }
}
