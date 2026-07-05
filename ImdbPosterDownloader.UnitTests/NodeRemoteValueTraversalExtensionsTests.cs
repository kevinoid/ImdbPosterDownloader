// <copyright file="NodeRemoteValueTraversalExtensionsTests.cs" company="Kevin Locke">
// Copyright 2019-2026 Kevin Locke.  All rights reserved.
// </copyright>
// Written with the assistance of Claude

namespace ImdbPosterDownloader.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using OpenQA.Selenium.BiDi.Script;
    using Xunit;

    public class NodeRemoteValueTraversalExtensionsTests
    {
        [Fact]
        public void GetDescendants_SingleNode_ReturnsEmptySequence()
        {
            var root = CreateNode("root");

            var result = root.GetDescendants();

            Assert.Empty(result);
        }

        [Fact]
        public void GetDescendants_NullRoot_ThrowsArgumentNull()
        {
            NodeRemoteValue? root = null;

            Assert.Throws<ArgumentNullException>(root!.GetDescendants);
        }

        [Fact]
        public void GetDescendants_NodeWithNullValue_ReturnsEmptySequence()
        {
            var root = new NodeRemoteValue("root", null);

            var result = root.GetDescendants();

            Assert.Empty(result);
        }

        [Fact]
        public void GetDescendants_EmptyChildrenArray_ReturnsEmptySequence()
        {
            var root = CreateNode("root", children: []);

            var result = root.GetDescendants();

            Assert.Empty(result);
        }

        [Fact]
        public void GetDescendants_ChildWithNullValue_ReturnsChild()
        {
            var child = new NodeRemoteValue("child", null);
            var root = CreateNode("root", children: [child]);

            var result = root.GetDescendants();

            Assert.Equal(["child"], SharedIds(result));
        }

        [Fact]
        public void GetDescendants_LinearChain_VisitsParentBeforeChild()
        {
            var grandchild = CreateNode("grandchild");
            var child = CreateNode("child", children: [grandchild]);
            var root = CreateNode("root", children: [child]);

            var result = root.GetDescendants();

            Assert.Equal(["child", "grandchild"], SharedIds(result));
        }

        [Fact]
        public void GetDescendants_MultipleChildren_VisitsLeftSubtreeBeforeNextSibling()
        {
            // root
            // ├── child1
            // │    └── grandchild1
            // └── child2
            var grandchild1 = CreateNode("grandchild1");
            var child1 = CreateNode("child1", children: [grandchild1]);
            var child2 = CreateNode("child2");
            var root = CreateNode("root", children: [child1, child2]);

            var result = root.GetDescendants();

            Assert.Equal(["child1", "grandchild1", "child2"], SharedIds(result));
        }

        [Fact]
        public void GetDescendants_ReturnsExpectedCount_ForBalancedTree()
        {
            // A 3-level binary tree: 1 root + 2 children + 4 grandchildren = 7 nodes.
            var grandchildren = Enumerable.Range(0, 4).Select(i => CreateNode($"gc{i}")).ToArray();
            var child1 = CreateNode("child1", children: [grandchildren[0], grandchildren[1]]);
            var child2 = CreateNode("child2", children: [grandchildren[2], grandchildren[3]]);
            var root = CreateNode("root", children: [child1, child2]);

            var result = root.GetDescendants();

            Assert.Equal(["child1", "gc0", "gc1", "child2", "gc2", "gc3"], SharedIds(result));
        }

        [Fact]
        public void GetDescendants_DeepLinearChain_DoesNotStackOverflow()
        {
            const int depth = 50_000;

            NodeRemoteValue current = CreateNode($"node${depth - 1}");
            for (int i = depth - 2; i >= 0; i--)
            {
                current = CreateNode($"node{i}", children: [current]);
            }

            var count = current.GetDescendants().Count();

            Assert.Equal(depth - 1, count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [SuppressMessage(
            "ErrorProne.NET",
            "EPS06:Expression causes a hidden copy of a struct",
            Justification = "Can't make ImmutableArray readonly")]
        public void GetDescendants_FlatChildren_VisitsAllInDeclaredOrder(int childCount)
        {
            var children = Enumerable.Range(0, childCount)
                .Select(i => CreateNode($"child{i}"))
                .ToImmutableArray();
            var root = CreateNode("root", children: children);

            var result = root.GetDescendants();

            var expected = children.Select(c => c.SharedId);
            Assert.Equal(expected, SharedIds(result));
        }

        [Fact]
        public void GetDescendantsOrSelf_SingleNode_ReturnsOnlyThatNode()
        {
            var root = CreateNode("root");

            var result = root.GetDescendantsOrSelf();

            var single = Assert.Single(result);
            Assert.Same(root, single);
        }

        [Fact]
        public void GetDescendantsOrSelf_NullRoot_ThrowsArgumentNull()
        {
            NodeRemoteValue? root = null;

            Assert.Throws<ArgumentNullException>(root!.GetDescendantsOrSelf);
        }

        [Fact]
        public void GetDescendantsOrSelf_NodeWithNullValue_IsTreatedAsLeaf()
        {
            var root = new NodeRemoteValue("root", null);

            var result = root.GetDescendantsOrSelf();

            var single = Assert.Single(result);
            Assert.Same(root, single);
        }

        [Fact]
        public void GetDescendantsOrSelf_EmptyChildrenArray_IsTreatedAsLeaf()
        {
            var root = CreateNode("root", children: []);

            var result = root.GetDescendantsOrSelf();

            Assert.Single(result);
        }

        [Fact]
        public void GetDescendantsOrSelf_ChildWithNullValue_ReturnsChild()
        {
            var child = new NodeRemoteValue("child", null);
            var root = CreateNode("root", children: [child]);

            var result = root.GetDescendantsOrSelf();

            Assert.Equal(["root", "child"], SharedIds(result));
        }

        [Fact]
        public void GetDescendantsOrSelf_LinearChain_VisitsParentBeforeChild()
        {
            var grandchild = CreateNode("grandchild");
            var child = CreateNode("child", children: [grandchild]);
            var root = CreateNode("root", children: [child]);

            var result = root.GetDescendantsOrSelf();

            Assert.Equal(["root", "child", "grandchild"], SharedIds(result));
        }

        [Fact]
        public void GetDescendantsOrSelf_MultipleChildren_VisitsLeftSubtreeBeforeNextSibling()
        {
            // root
            // ├── child1
            // │    └── grandchild1
            // └── child2
            var grandchild1 = CreateNode("grandchild1");
            var child1 = CreateNode("child1", children: [grandchild1]);
            var child2 = CreateNode("child2");
            var root = CreateNode("root", children: [child1, child2]);

            var result = root.GetDescendantsOrSelf();

            Assert.Equal(["root", "child1", "grandchild1", "child2"], SharedIds(result));
        }

        [Fact]
        public void GetDescendantsOrSelf_ReturnsExpectedCount_ForBalancedTree()
        {
            // A 3-level binary tree: 1 root + 2 children + 4 grandchildren = 7 nodes.
            var grandchildren = Enumerable.Range(0, 4).Select(i => CreateNode($"gc{i}")).ToArray();
            var child1 = CreateNode("child1", children: [grandchildren[0], grandchildren[1]]);
            var child2 = CreateNode("child2", children: [grandchildren[2], grandchildren[3]]);
            var root = CreateNode("root", children: [child1, child2]);

            var result = root.GetDescendantsOrSelf();

            Assert.Equal(["root", "child1", "gc0", "gc1", "child2", "gc2", "gc3"], SharedIds(result));
        }

        [Fact]
        public void GetDescendantsOrSelf_DeepLinearChain_DoesNotStackOverflow()
        {
            const int depth = 50_000;

            NodeRemoteValue current = CreateNode($"node${depth - 1}");
            for (int i = depth - 2; i >= 0; i--)
            {
                current = CreateNode($"node{i}", children: [current]);
            }

            var count = current.GetDescendantsOrSelf().Count();

            Assert.Equal(depth, count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [SuppressMessage(
            "ErrorProne.NET",
            "EPS06:Expression causes a hidden copy of a struct",
            Justification = "Can't make ImmutableArray readonly")]
        public void GetDescendantsOrSelf_FlatChildren_VisitsAllInDeclaredOrder(int childCount)
        {
            var children = Enumerable.Range(0, childCount)
                .Select(i => CreateNode($"child{i}"))
                .ToImmutableArray();
            var root = CreateNode("root", children: children);

            var result = root.GetDescendantsOrSelf();

            IEnumerable<string> expected = ["root", .. children.Select(c => c.SharedId)];
            Assert.Equal(expected, SharedIds(result));
        }

        [SuppressMessage(
            "Roslynator",
            "RCS1242:Do not pass non-read-only struct by read-only reference",
            Justification = "Nullable<T> is nearly readonly: https://github.com/dotnet/runtime/issues/23969")]
        private static NodeRemoteValue CreateNode(
            string sharedId,
            string? localName = null,
            in ImmutableArray<NodeRemoteValue>? children = null)
        {
            var properties = new NodeProperties(
                NodeType: 1,
                ChildNodeCount: children?.Length ?? 0,
                Attributes: null,
                Children: children,
                LocalName: localName,
                Mode: null,
                NamespaceUri: null,
                NodeValue: null,
                ShadowRoot: null);

            return new NodeRemoteValue(sharedId, properties);
        }

        private static IEnumerable<string> SharedIds(IEnumerable<NodeRemoteValue> nodes) =>
            nodes.Select(n => n.SharedId);
    }
}
