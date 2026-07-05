// <copyright file="NodeRemoteValueTraversalExtensions.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>
// Written with the assistance of Claude

namespace ImdbPosterDownloader
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OpenQA.Selenium.BiDi.Script;

    /// <summary>
    /// Extension methods for traversing <see cref="NodeRemoteValue"/>s.
    /// </summary>
    internal static class NodeRemoteValueTraversalExtensions
    {
        /// <summary>
        /// Gets the descendants of a given node in pre-order (node-then-children, left-to-right).
        /// </summary>
        /// <remarks>
        /// Traversal may be shallower than expected when <see cref="NodeRemoteValue.Value"/> or
        /// <see cref="NodeProperties.Children"/> are <c>null</c> due to
        /// <see cref="SerializationOptions.MaxDomDepth"/>.
        /// </remarks>
        /// <param name="root">Root of the node hierarchy to traverse.</param>
        /// <returns><paramref name="root"/> followed by all descendants in pre-order.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="root"/> is
        /// /// <c>null</c>.</exception>
        public static IEnumerable<NodeRemoteValue> GetDescendants(this NodeRemoteValue root)
        {
            ArgumentNullException.ThrowIfNull(root);
            return GetDescendantsInternal(root, false);
        }

        /// <summary>
        /// Gets the descendants of a given node, including the node itself, in pre-order
        /// (node-then-children, left-to-right).
        /// </summary>
        /// <remarks>
        /// Traversal may be shallower than expected when <see cref="NodeRemoteValue.Value"/> or
        /// <see cref="NodeProperties.Children"/> are <c>null</c> due to
        /// <see cref="SerializationOptions.MaxDomDepth"/>.
        /// </remarks>
        /// <param name="root">Root of the node hierarchy to traverse.</param>
        /// <returns><paramref name="root"/> followed by all descendants in pre-order.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="root"/> is
        /// /// <c>null</c>.</exception>
        public static IEnumerable<NodeRemoteValue> GetDescendantsOrSelf(this NodeRemoteValue root)
        {
            ArgumentNullException.ThrowIfNull(root);
            return GetDescendantsInternal(root, true);
        }

        private static IEnumerable<NodeRemoteValue> GetDescendantsInternal(
            NodeRemoteValue root,
            bool includeRoot)
        {
            if (includeRoot)
            {
                yield return root;
            }

            var rootChildren = root.Value?.Children;
            if (rootChildren is not { IsDefaultOrEmpty: false } rootChildArray)
            {
                yield break;
            }

            // Push in reverse so the first child is popped (visited) first.
            var stack = new Stack<NodeRemoteValue>(rootChildArray.Reverse());
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                yield return current;

                var children = current.Value?.Children;
                if (children is not { IsDefaultOrEmpty: false } childArray)
                {
                    continue;
                }

                // Push in reverse so the first child is popped (visited) first.
                foreach (var child in childArray.Reverse())
                {
                    stack.Push(child);
                }
            }
        }
    }
}
