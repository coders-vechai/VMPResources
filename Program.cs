using dnlib.DotNet;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
namespace VMPResources
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Assembly _assembly = Assembly.LoadFrom(args[0]);
            ModuleDefMD _module = ModuleDefMD.Load(args[0]);
            RuntimeHelpers.RunModuleConstructor(_assembly.ManifestModule.ModuleHandle);
            foreach (AssemblyName _assemblyName in _assembly.GetReferencedAssemblies())
            {
                Assembly _referencedAssembly = Assembly.Load(_assemblyName);
                if (_referencedAssembly == null) continue;
                foreach (string _resourceName in _referencedAssembly.GetManifestResourceNames())
                {
                    using (Stream _resourceStream = _referencedAssembly.GetManifestResourceStream(_resourceName))
                    {
                        List<Resource> resourceS = new List<Resource>();
                        foreach (var _resourcess in _module.Resources.ToList())
                        {
                            if (_resourcess.Name != _resourceName) continue;
                            byte[] _resourceBytes;
                            using (var ms = new MemoryStream())
                            {
                                _resourceStream.CopyTo(ms);
                                _resourceBytes = ms.ToArray();
                            }
                            EmbeddedResource embeddedResource = new EmbeddedResource(_resourceName, _resourceBytes, ManifestResourceAttributes.Public);
                            resourceS.Add(embeddedResource);
                            AssemblyLinkedResource existingResource = _module.Resources.FindAssemblyLinkedResource(_resourceName);
                            _module.Resources.Remove(existingResource);
                        }
                        foreach (var fixedRes in resourceS)
                        {
                            _module.Resources.Add(fixedRes);
                        }
                    }
                }
            }
            string _filePath = Path.GetDirectoryName(_module.Location);
            string _fileName = Path.GetFileNameWithoutExtension(_module.Location);
            string _newName = _fileName + "-decrypted" + Path.GetExtension(_module.Location);

            var _NativemoduleWriterOptions = new NativeModuleWriterOptions(_module, false);
            _NativemoduleWriterOptions.MetadataOptions.Flags = MetadataFlags.PreserveAll;
            _NativemoduleWriterOptions.MetadataLogger = DummyLogger.NoThrowInstance;
            _module.NativeWrite(Path.Combine(_filePath, _newName), _NativemoduleWriterOptions);

            Console.WriteLine($"File saved in: {Path.Combine(_filePath, _newName)}");
            Console.ReadKey();
        }
    }
}
