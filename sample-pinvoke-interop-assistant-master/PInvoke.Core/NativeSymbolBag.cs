// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static PInvoke.Contract;
using PInvoke.Parser;

namespace PInvoke
{
    /// <summary>
    /// An <see cref="INativeSymbolStorage"/>  which is capable of doing name resolution.
    /// </summary>
    public sealed class NativeSymbolBag : INativeSymbolBag
    {
        private struct ResolveResult
        {
            internal bool StepSucceeded { get; }
            internal bool NeedMoreWork { get; }

            internal ResolveResult(bool stepSucceeded, bool needMoreWork)
            {
                StepSucceeded = stepSucceeded;
                NeedMoreWork = needMoreWork;
            }
        }

        private readonly BasicSymbolStorage _storage;
        private readonly INativeSymbolLookup _nextSymbolLookup;

        public static INativeSymbolLookup EmptyLookup => EmptyNativeSymbolBag.Instance;

        public int Count => _storage.Count;
        public IEnumerable<NativeName> NativeNames => _storage.NativeNames;
        public IEnumerable<NativeDefinedType> NativeDefinedTypes => _storage.NativeDefinedTypes;
        public IEnumerable<NativeTypeDef> NativeTypeDefs => _storage.NativeTypeDefs;
        public IEnumerable<NativeProcedure> NativeProcedures => _storage.NativeProcedures;
        public IEnumerable<NativeConstant> NativeConstants => _storage.NativeConstants;
        public IEnumerable<NativeEnumValue> NativeEnumValues => _storage.NativeEnumValues;

        public BasicSymbolStorage Storage => _storage;
        public INativeSymbolLookup NextSymbolLookup => _nextSymbolLookup;

        public NativeSymbolBag(INativeSymbolLookup nextSymbolBag = null)
        {
            _storage = new BasicSymbolStorage();
            _nextSymbolLookup = nextSymbolBag ?? EmptyLookup;
        }

        public bool TryGetGlobalSymbol(string name, out NativeGlobalSymbol symbol) =>
            _storage.TryGetGlobalSymbol(name, out symbol) ||
            _nextSymbolLookup.TryGetGlobalSymbol(name, out symbol);

        public bool TryGetGlobalSymbol(NativeName name, out NativeGlobalSymbol symbol) =>
            _storage.TryGetGlobalSymbol(name, out symbol) ||
            _nextSymbolLookup.TryGetGlobalSymbol(name, out symbol);

        public void Add(NativeGlobalSymbol symbol) => _storage.Add(symbol);

        /// <summary>
        /// Find the resolved symbols
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        public IEnumerable<NativeSymbol> FindResolvedNativeSymbols()
        {
            List<NativeSymbol> list = new List<NativeSymbol>();

            foreach (NativeDefinedType definedNt in FindResolvedDefinedTypes())
            {
                list.Add(definedNt);
            }

            foreach (NativeTypeDef typedef in FindResolvedTypeDefs())
            {
                list.Add(typedef);
            }

            foreach (NativeConstant c in this.FindResolvedConstants())
            {
                list.Add(c);
            }

            foreach (NativeProcedure proc in this.FindResolvedProcedures())
            {
                list.Add(proc);
            }

            return list;
        }

        /// <summary>
        /// find all of the reachable NativeSymbol instances in this bag
        /// </summary>
        public List<NativeSymbol> FindAllReachableNativeSymbols()
        {
            var list = new List<NativeSymbol>();
            foreach (var cur in FindAllReachableNativeSymbolRelationships())
            {
                list.Add(cur.Symbol);
            }

            return list;
        }

        public List<NativeSymbolRelationship> FindAllReachableNativeSymbolRelationships()
        {
            // Build up the list of types
            List<NativeSymbol> list = new List<NativeSymbol>();
            foreach (var definedNt in NativeDefinedTypes)
            {
                list.Add(definedNt);
            }

            foreach (NativeTypeDef typedefNt in NativeTypeDefs)
            {
                list.Add(typedefNt);
            }

            foreach (NativeProcedure proc in NativeProcedures)
            {
                list.Add(proc);
            }

            foreach (NativeConstant c in NativeConstants)
            {
                list.Add(c);
            }

            NativeSymbolIterator iter = new NativeSymbolIterator();
            return iter.FindAllNativeSymbolRelationships(list);
        }

        /// <summary>
        /// Find all of the NativeNamedType instances for which a type could not be found
        /// </summary>
        public List<NativeSymbolRelationship> FindUnresolvedNativeSymbolRelationships()
        {
            List<NativeSymbolRelationship> list = new List<NativeSymbolRelationship>();
            foreach (NativeSymbolRelationship rel in this.FindAllReachableNativeSymbolRelationships())
            {
                if (!rel.Symbol.IsImmediateResolved)
                {
                    list.Add(rel);
                }
            }

            return list;
        }

        public List<NativeValue> FindUnresolvedNativeValues()
        {
            List<NativeValue> list = new List<NativeValue>();
            foreach (NativeSymbol ns in this.FindAllReachableNativeSymbols())
            {
                NativeValue nValue = ns as NativeValue;
                if (nValue != null && !nValue.IsValueResolved)
                {
                    list.Add(nValue);
                }
            }

            return list;
        }

        private bool TryFindTypeCore(string name, out NativeType type, out bool loadedFromNextLookup)
        {
            if (_storage.TryGetType(name, out type))
            {
                loadedFromNextLookup = false;
                return true;
            }

            if (_nextSymbolLookup.TryGetType(name, out type))
            {
                loadedFromNextLookup = true;
                return true;
            }

            NativeBuiltinType bt = null;
            if (NativeBuiltinType.TryConvertToBuiltinType(name, out bt))
            {
                loadedFromNextLookup = false;
                type = bt;
                return true;
            }

            loadedFromNextLookup = false;
            return false;
        }

        private bool TryGetValueCore(string name, out NativeSymbol symbol, out bool loadedFromNextLookup)
        {
            if (_storage.TryGetValue(name, out symbol))
            {
                loadedFromNextLookup = false;
                return true;
            }

            if (_nextSymbolLookup.TryGetValue(name, out symbol))
            {
                loadedFromNextLookup = true;
                return true;
            }

            loadedFromNextLookup = false;
            return false;
        }

        /// <summary>
        /// Try and resolve this <see cref="NativeNamedType"/> to a real type or at least
        /// the next level.
        /// </summary>
        public bool TryResolveNamedType(NativeNamedType namedType, out NativeType type)
        {
            bool loadedFromNextLookup;
            return TryResolveNamedTypeCore(namedType, out type, out loadedFromNextLookup);
        }

        private bool TryResolveNamedTypeCore(NativeNamedType namedType, out NativeType type, out bool loadedFromNextLookup)
        {
            if (string.IsNullOrEmpty(namedType.Qualification))
            {
                // If there is no qualification then just load the type by it's name
                return this.TryFindTypeCore(namedType.Name, out type, out loadedFromNextLookup);
            }

            // When there is a qualification it is either struct, union or enum.  Try and load the defined type
            // for the name and then make sure that it is the correct type 
            if (!TryFindTypeCore(namedType.Name, out type, out loadedFromNextLookup))
            {
                type = null;
                return false;
            }

            var definedNt = type as NativeDefinedType;
            if (definedNt == null)
            {
                type = null;
                return false;
            }

            string typeQualification = null;
            switch (definedNt.Kind)
            {
                case NativeSymbolKind.StructType:
                    typeQualification = "struct";
                    break;
                case NativeSymbolKind.UnionType:
                    typeQualification = "union";
                    break;
                case NativeSymbolKind.EnumType:
                    typeQualification = "enum";
                    break;
                default:
                    return false;
            }

            string qual = namedType.Qualification;
            if ("class" == qual)
            {
                qual = "struct";
            }

            return typeQualification == qual;
        }

        /// <summary>
        /// Save all of the information into a NativeStorage database that is completely resolved
        /// TODO: INativeSymbolStorage is wrong.  Should have a lower API like INativeSymbolLookupRaw.
        /// </summary>
        public void SaveToNativeStorage(INativeSymbolStorage nativeStorage)
        {
            foreach (NativeConstant nConst in this.FindResolvedConstants())
            {
                nativeStorage.AddConstant(nConst);
            }

            foreach (NativeDefinedType definedNt in this.FindResolvedDefinedTypes())
            {
                nativeStorage.AddDefinedType(definedNt);
            }

            foreach (NativeTypeDef typeDef in this.FindResolvedTypeDefs())
            {
                nativeStorage.AddTypeDef(typeDef);
            }

            foreach (NativeProcedure proc in this.FindResolvedProcedures())
            {
                nativeStorage.AddProcedure(proc);
            }
        }

        /// <summary>
        /// Find all of the resolved defined types.  
        /// </summary>
        public IEnumerable<NativeDefinedType> FindResolvedDefinedTypes()
        {
            var map = new Dictionary<NativeSymbol, bool?>();
            var list = new List<NativeDefinedType>();

            foreach (var definedNt in _storage.NativeDefinedTypes)
            {
                if (IsResolved(definedNt, map))
                {
                    list.Add(definedNt);
                }
            }

            return list;
        }

        /// <summary>
        /// Find all of the resolved typedefs
        /// </summary>
        public IEnumerable<NativeTypeDef> FindResolvedTypeDefs()
        {
            var map = new Dictionary<NativeSymbol, bool?>();
            var list = new List<NativeTypeDef>();

            foreach (var typedefNt in NativeTypeDefs)
            {
                if (IsResolved(typedefNt, map))
                {
                    list.Add(typedefNt);
                }
            }

            return list;
        }

        /// <summary>
        /// Find all of the resolved NativeProcedure instances
        /// </summary>
        public IEnumerable<NativeProcedure> FindResolvedProcedures()
        {
            var map = new Dictionary<NativeSymbol, bool?>();
            var list = new List<NativeProcedure>();

            foreach (var proc in NativeProcedures)
            {
                if (IsResolved(proc, map))
                {
                    list.Add(proc);
                }
            }

            return list;
        }

        public IEnumerable<NativeConstant> FindResolvedConstants()
        {
            var map = new Dictionary<NativeSymbol, bool?>();
            var list = new List<NativeConstant>();

            foreach (var c in NativeConstants)
            {
                if (IsResolved(c, map))
                {
                    list.Add(c);
                }
            }

            return list;
        }

        public bool TryResolveSymbolsAndValues()
        {
            using (ProcedureFinder finder = new ProcedureFinder())
            {
                return TryResolveSymbolsAndValues(finder, new ErrorProvider());
            }
        }

        public bool TryResolveSymbolsAndValues(ErrorProvider ep)
        {
            using (ProcedureFinder finder = new ProcedureFinder())
            {
                return TryResolveSymbolsAndValues(finder, ep);
            }
        }

        /// <summary>
        /// Try and resolve all of the unresolved types in the bag.  Return false if the types
        /// couldn't all be resolved
        /// </summary>
        public bool TryResolveSymbolsAndValues(ProcedureFinder finder, ErrorProvider ep)
        {
            // Try and resolve the proc name
            foreach (var proc in NativeProcedures)
            {
                if (string.IsNullOrEmpty(proc.DllName))
                {
                    finder.TryFindDllNameExact(proc.Name, out proc.DllName);
                }
            }

            return ResolveCore(ep);
        }

        private bool ResolveCore(ErrorProvider ep)
        {
            var allResolved = true;
            do
            {
                var symbolResult = ResolveCoreSymbols(ep);
                var valueResult = ResolveCoreValues(ep);
                allResolved = allResolved && symbolResult.StepSucceeded && valueResult.StepSucceeded;

                if (!symbolResult.NeedMoreWork && !valueResult.NeedMoreWork)
                {
                    break;
                }

            } while (true);

            return allResolved;
        }

        /// <summary>
        /// Try and resolve the unresolved symbols in the system
        /// </summary>
        private ResolveResult ResolveCoreSymbols(ErrorProvider ep)
        {
            var succeeded = true;
            var needMoreWork = false;

            foreach (NativeSymbolRelationship rel in this.FindUnresolvedNativeSymbolRelationships())
            {
                // Values and value expressions are resolved in a different pass.
                if (rel.Symbol.Kind == NativeSymbolKind.Value || rel.Symbol.Kind == NativeSymbolKind.ValueExpression)
                {
                    continue;
                }

                // In this pass we are resolving the named type instances.
                NativeNamedType namedType = rel.Symbol as NativeNamedType;
                if (namedType == null)
                {
                    ep.AddError($"Failed to resolve {rel.Symbol.Kind} -> '{rel.Symbol.DisplayName}'");
                    continue;
                }

                NativeType nt = null;
                bool loadedFromNextLookup;
                if (TryResolveNamedTypeCore(namedType, out nt, out loadedFromNextLookup))
                {
                    Debug.Assert(nt != null);
                    namedType.RealType = nt;
                    needMoreWork = true;
                }
                else if (rel.Parent != null && rel.Parent.Kind == NativeSymbolKind.PointerType && !string.IsNullOrEmpty(namedType.Qualification))
                {
                    // When we have a pointer to an unresolved type, treat this as an opaque type
                    ep.AddWarning($"Treating '{namedType.DisplayName}' as pointer to opaque type");
                    namedType.RealType = new NativeOpaqueType();
                }
                else
                {
                    ep.AddError($"Failed to resolve name '{namedType.DisplayName}'");
                    succeeded = false;
                }
            }

            return new ResolveResult(succeeded, needMoreWork);
        }

        /// <summary>
        /// Try and resolve the unresolved values in the system.  
        /// </summary>
        private ResolveResult ResolveCoreValues(ErrorProvider ep)
        {
            var succeeded = true;
            var needMoreWork = false;
            foreach (NativeValue nValue in this.FindUnresolvedNativeValues())
            {
                var loadedFromNextLookup = false;
                switch (nValue.ValueKind)
                {
                    case NativeValueKind.SymbolValue:
                        {
                            NativeSymbol symbol = null;
                            if (TryGetValueCore(nValue.Name, out symbol, out loadedFromNextLookup))
                            {
                                nValue.Value = symbol;
                            }
                        }
                        break;
                    case NativeValueKind.SymbolType:
                        {
                            NativeType type = null;
                            if (TryFindTypeCore(nValue.Name, out type, out loadedFromNextLookup))
                            {
                                nValue.Value = type;
                            }
                        }
                        break;
                }

                if (!nValue.IsImmediateResolved)
                {
                    ep.AddError($"Failed to resolve value '{nValue.Name}'");
                    succeeded = false;
                }

                if (loadedFromNextLookup)
                {
                    needMoreWork = true;
                }
            }

            return new ResolveResult(succeeded, needMoreWork);
        }

        private bool IsResolved(NativeSymbol ns, Dictionary<NativeSymbol, bool?> map)
        {
            ThrowIfNull(ns);
            ThrowIfNull(map);

            // See if this has already been calculated
            bool? ret = false;
            if (map.TryGetValue(ns, out ret))
            {
                if (ret.HasValue)
                {
                    return ret.Value;
                }
                else
                {
                    // We're in a recursive call to the same type.  Return true here because if another type is
                    // not resolved then this will fall out
                    return true;
                }
            }

            // If there are no immediate children then the type is most definately resolved
            NativeSymbolIterator it = new NativeSymbolIterator();
            List<NativeSymbol> children = new List<NativeSymbol>(ns.GetChildren());
            if (children.Count == 0)
            {
                return true;
            }

            // Add an entry into the map to indicate that we are exploring this type
            map.Add(ns, null);

            ret = true;
            foreach (NativeSymbol child in children)
            {
                if (!child.IsImmediateResolved || !IsResolved(child, map))
                {
                    ret = false;
                    break;
                }
            }

            // Save the success
            map[ns] = ret;
            return ret.Value;
        }

        public static string GenerateAnonymousName()
        {
            Guid g = Guid.NewGuid();
            string name = g.ToString();
            name = name.Replace("-", "_");
            return "Anonymous_" + name;
        }

        public static bool IsAnonymousName(string name)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(name, "^Anonymous_((\\w+_){4})(\\w+)$");
        }

        public static NativeSymbolBag CreateFrom(Parser.NativeCodeAnalyzerResult result, INativeSymbolStorage storage)
        {
            return CreateFrom(result, storage, new ErrorProvider());
        }

        /// <summary>
        /// Create a NativeTypeBag from the result of a code analysis
        /// </summary>
        public static NativeSymbolBag CreateFrom(NativeCodeAnalyzerResult result, INativeSymbolLookup nextSymbolBag, ErrorProvider ep)
        {
            var bag = new NativeSymbolBag(nextSymbolBag);
            foreach (var symbol in result.Symbols)
            {
                try
                {
                    bag.Add(symbol);
                }
                catch
                {
                    ep.AddError($"Error adding symbol {symbol.Name.Name}");
                }
            }
            return bag;
        }
    }
}
