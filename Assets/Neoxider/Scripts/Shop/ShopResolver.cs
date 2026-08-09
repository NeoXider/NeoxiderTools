using UnityEngine;

namespace Neo.Shop
{
    /// <summary>
    ///     Shared fallback resolver for Shop scene references. Searches the context component's
    ///     parent hierarchy first, then falls back to the first instance found in the scene.
    ///     Replaces the duplicated <c>GetComponentInParent&lt;T&gt;() ?? FindFirstObjectByType&lt;T&gt;()</c>
    ///     pattern across shop views and buttons.
    /// </summary>
    public static class ShopResolver
    {
        /// <summary>
        ///     Resolves a component of type <typeparamref name="T" /> from the parent hierarchy of
        ///     <paramref name="context" />, falling back to the first instance in the scene.
        /// </summary>
        /// <param name="context">Component whose hierarchy is searched first.</param>
        /// <returns>The resolved component, or null when none exists.</returns>
        public static T Resolve<T>(Component context) where T : Component
        {
            if (context == null)
            {
                return null;
            }

            T result = context.GetComponentInParent<T>();
            if (result == null)
            {
                result = Object.FindFirstObjectByType<T>();
            }

            return result;
        }

        /// <summary>
        ///     Tries to resolve a component of type <typeparamref name="T" /> from the parent hierarchy of
        ///     <paramref name="context" />, falling back to the first instance in the scene.
        /// </summary>
        /// <param name="context">Component whose hierarchy is searched first.</param>
        /// <param name="result">The resolved component, or null when none exists.</param>
        /// <returns>True when a component was found.</returns>
        public static bool TryResolve<T>(Component context, out T result) where T : Component
        {
            result = Resolve<T>(context);
            return result != null;
        }

        /// <summary>
        ///     Resolves the scene <see cref="Shop" /> from the parent hierarchy of
        ///     <paramref name="context" />, falling back to the first <see cref="Shop" /> in the scene.
        /// </summary>
        /// <param name="context">Component whose hierarchy is searched first.</param>
        /// <returns>The resolved shop, or null when none exists.</returns>
        public static Shop ResolveShop(Component context)
        {
            return Resolve<Shop>(context);
        }
    }
}
