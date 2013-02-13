using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ILStrip
{
    class Program
    {
        static List<string> _typeIdsFound = new List<string>();

        static void Main(string[] args)
        {
            var assembly = AssemblyDefinition.ReadAssembly(@"C:\Data\GK\Dev\_Temp\ilstrip\GkdcAwsToolsMerged.dll");
            var allTypes = assembly.MainModule.Types;

            _typeIdsFound.Add(".<Module>");
            foreach (var typeDef in allTypes.Where(t => t.IsPublic))
                AddScanType(typeDef);

            foreach (var typeDef in allTypes.Where(t => !_typeIdsFound.Contains(GetTypeId(t))).ToList())
            {
                //Console.WriteLine("Removing: {0}", typeDef);
                allTypes.Remove(typeDef);
            }

            assembly.Write(@"C:\Data\GK\Dev\_Temp\ilstrip\GkdcAwsToolsMergedNew.dll");
        }
        
        
        static int _addScanTypeRecursionLevel = -1;

        static void AddScanType(TypeReference typeRef)
        {
            if (typeRef == null || _typeIdsFound.Contains(GetTypeId(typeRef)))
                return;

            _addScanTypeRecursionLevel++;
            Console.WriteLine(string.Format("Processing: {0}{1}", "".PadRight(_addScanTypeRecursionLevel * 2), typeRef.FullName));

            _typeIdsFound.Add(GetTypeId(typeRef));

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
                
                types = types.Union(types.Select(f => f as GenericInstanceType).Where(g => g != null).SelectMany(g => g.GenericArguments));

                foreach (var t in types)
                    AddScanType(t);
            }

            _addScanTypeRecursionLevel--;
        }

        static string GetTypeId(TypeReference typeRef)
        {
            return string.Format("{0}.{1}", typeRef.Namespace, typeRef.Name);
        }
    }
}
