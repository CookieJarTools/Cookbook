using UnityEditor;
using UnityEngine;

namespace CookieJarTools.Cookbook.Samples.Editor
{
    /// <summary>
    /// Actions and a check for the CookBook's step-by-step guide. One action lets the reader choose
    /// where the new asset goes; the other puts it where the programmer decided. Both return what
    /// they made, so the CookBook selects it afterwards. The check says whether the starter recipe
    /// is there yet.
    /// </summary>
    public static class CookbookSampleActions
    {
        private const string SetFolder = "Assets/Cookbook Example";

        [CookbookAction("Make an example recipe", Id = "example-recipe", Description = "Makes a new Example Recipe asset — you choose where it goes.", DefaultName = "My Recipe")]
        public static Object MakeRecipe(string path)
        {
            var recipe = ScriptableObject.CreateInstance<ExampleRecipe>();
            AssetDatabase.CreateAsset(recipe, AssetDatabase.GenerateUniqueAssetPath(path));
            AssetDatabase.SaveAssets();
            return recipe;
        }

        [CookbookCheck("The starter recipe exists", Id = "has-starter-recipe", Description = "Looks for Starter Recipe in " + SetFolder + ".")]
        public static string HasStarterRecipe()
        {
            return AssetDatabase.FindAssets("t:ExampleRecipe Starter", new[] { "Assets" }).Length > 0 ? null : "No starter recipe yet — run the step above";
        }

        [CookbookAction("Make the starter recipe", Id = "starter-recipe", Description = "Makes Starter Recipe in " + SetFolder + ", where the project keeps it.")]
        public static Object MakeStarterRecipe()
        {
            if (!AssetDatabase.IsValidFolder(SetFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Cookbook Example");
            }

            var recipe = ScriptableObject.CreateInstance<ExampleRecipe>();
            AssetDatabase.CreateAsset(recipe, AssetDatabase.GenerateUniqueAssetPath(SetFolder + "/Starter Recipe.asset"));
            AssetDatabase.SaveAssets();
            return recipe;
        }
    }
}
