using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using ME.ECSBurst;

namespace ME.ECSBurstEditor {

    public class Generator : AssetPostprocessor {

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {

            foreach (var asset in deletedAssets) {
                
                if (asset.EndsWith(".cs") == true && asset.EndsWith("components.gen.cs") == false) {
            
                    Generator.Generate();
                    return;

                }

            }

            foreach (var asset in importedAssets) {

                if (asset.EndsWith(".cs") == true && asset.EndsWith("components.gen.cs") == false) {
            
                    Generator.Generate();
                    return;

                }
                
            }
            
        }

        public static void Generate() {

            var dir = Generator.FindTargetDir();
            if (string.IsNullOrEmpty(dir) == false) {
                
                Generator.Generate(dir);
                Debug.Log("Done");
                
            } else {
                
                Debug.LogWarning("me.ecsburst.generator.txt file not found");
                
            }

        }

        public static string FindTargetDir() {

            var assets = AssetDatabase.FindAssets("t:TextAsset");
            foreach (var guid in assets) {

                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset.name == "me.ecsburst.generator") {

                    return System.IO.Path.GetDirectoryName(path);

                }

            }

            return null;

        }

        public static System.Type[] GetAllComponents() {

            return System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => {

                var allTypes = x.GetTypes();
                return allTypes.Where(y => y.IsValueType == true && typeof(IComponentBase).IsAssignableFrom(y)).ToArray();
                
            }).ToArray();

        }

        public static void Generate(string dir) {

            var text = @"
    using ME.ECSBurst;

    public partial struct ComponentsGen {

        static partial void Init() {
            
            //WorldUtilities.UpdateAllComponentTypeId<...>();
#INIT#
            
        }

        static partial void Init(ref World world) {
            
            //world.Validate<...>();
#INITWORLD#
            
        }
        
    }
    ";

            var sourceInit = "            WorldUtilities.UpdateAllComponentTypeId<#TYPE#>();\n";
            var sourceInitWorld = "            world.Validate<#TYPE#>();\n";

            var init = string.Empty;
            var initWorld = string.Empty;
            var allComponents = Generator.GetAllComponents();
            var listInit = new List<string>();
            var listWorldInit = new List<string>();
            foreach (var component in allComponents) {

                var cName = component.FullName;
                cName = cName.Replace("+", ".");

                listInit.Add(sourceInit.Replace("#TYPE#", cName));
                listWorldInit.Add(sourceInitWorld.Replace("#TYPE#", cName));

            }

            init = string.Join("", listInit);
            initWorld = string.Join("", listWorldInit);

            text = text.Replace("#INIT#", init);
            text = text.Replace("#INITWORLD#", initWorld);

            System.IO.File.WriteAllText(dir + "/components.gen.cs", text);

        }

    }

}