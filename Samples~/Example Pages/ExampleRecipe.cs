using UnityEngine;

namespace CookieJarTools.Cookbook.Samples
{
    /// <summary>A small asset for the step-by-step guide to make: the Cookbook's "Create an asset" action works with any ScriptableObject like it.</summary>
    [CreateAssetMenu(menuName = "CookieJarTools/Cookbook/Example Recipe")]
    public sealed class ExampleRecipe : ScriptableObject
    {
        [SerializeField] private string dish = "Cookies";
        [SerializeField] private int servings = 12;
        [SerializeField] private float bakeMinutes = 11f;
    }
}
