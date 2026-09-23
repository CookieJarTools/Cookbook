using CookieJarTools.Frosting;
using UnityEngine;

namespace CookieJarTools.Cookbook.Samples
{
    /// <summary>
    /// [Documentation] on a component and on a field. In a Frosting window, the title bar and the
    /// field each get a book button; in Unity's inspector, the header gets a Docs button and the
    /// field a link above it.
    /// </summary>
    [Frosting]
    [Documentation("a4f08c3e91b2")]
    public sealed class DocumentedExample : MonoBehaviour
    {
        [Documentation("Cookbook Guide/Writing Pages")]
        [SerializeField] private int damage = 10;

        [SerializeField] private float speed = 4f;
    }
}
