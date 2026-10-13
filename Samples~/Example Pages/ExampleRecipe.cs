using UnityEngine;

namespace CookieJarTools.Cookbook.Samples
{
    /// <summary>A small asset for the step-by-step guide to make: the CookBook's "Create an asset" action works with any ScriptableObject like it.</summary>
    [CreateAssetMenu(menuName = "CookieJarTools/CookBook/Example Recipe")]
    public sealed class ExampleRecipe : ScriptableObject
    {
        [SerializeField] private string dish = "Cookies";
        [SerializeField] private int servings = 12;
        [SerializeField] private float bakeMinutes = 11f;
    }
}
