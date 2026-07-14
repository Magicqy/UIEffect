using System.Collections.Generic;
using System.Linq;
using Coffee.UIEffects;
using Coffee.UIEffectInternal;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Coffee.UIEffect.Tests
{
    public class ShaderVariantRegistryTests
    {
        private Shader _first;
        private Shader _second;

        [SetUp]
        public void SetUp()
        {
            var shaders = AssetDatabase.FindAssets("t:Shader")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Shader>)
                .Where(shader => shader)
                .Distinct()
                .OrderBy(GetStableKey)
                .Take(2)
                .ToArray();

            Assert.That(shaders, Has.Length.EqualTo(2));
            _first = shaders[0];
            _second = shaders[1];
            Assert.That(_second, Is.Not.SameAs(_first));
        }

        [Test]
        public void SynchronizeRegisteredShaders_SameSetInDifferentOrder_DoesNotChangeList()
        {
            var owner = ScriptableObject.CreateInstance<UIEffectProjectSettings>();
            var registered = new List<Shader> { _second, _first };

            var changed = ShaderVariantRegistry.SynchronizeRegisteredShaders(
                owner, registered, new[] { _first, _second });

            Assert.That(changed, Is.False);
            Assert.That(registered, Is.EqualTo(new[] { _second, _first }));
            Assert.That(EditorUtility.IsDirty(owner), Is.False);
            Object.DestroyImmediate(owner);
        }

        [Test]
        public void SynchronizeRegisteredShaders_NewShader_ReplacesWithStableOrder()
        {
            var owner = ScriptableObject.CreateInstance<UIEffectProjectSettings>();
            var registered = new List<Shader> { _second };

            var changed = ShaderVariantRegistry.SynchronizeRegisteredShaders(
                owner, registered, new[] { _second, _first });

            Assert.That(changed, Is.True);
            Assert.That(registered, Is.EqualTo(new[] { _first, _second }));
            Assert.That(EditorUtility.IsDirty(owner), Is.True);
            Object.DestroyImmediate(owner);
        }

        [Test]
        public void SynchronizeRegisteredShaders_RemovedShader_UpdatesList()
        {
            var registered = new List<Shader> { _first, _second };

            var changed = ShaderVariantRegistry.SynchronizeRegisteredShaders(
                null, registered, new[] { _second });

            Assert.That(changed, Is.True);
            Assert.That(registered, Is.EqualTo(new[] { _second }));
        }

        [Test]
        public void SynchronizeRegisteredShaders_DuplicateShader_RemovesDuplicate()
        {
            var registered = new List<Shader> { _first, _first };

            var changed = ShaderVariantRegistry.SynchronizeRegisteredShaders(
                null, registered, new[] { _first, _first });

            Assert.That(changed, Is.True);
            Assert.That(registered, Is.EqualTo(new[] { _first }));
        }

        [Test]
        public void SynchronizeRegisteredShaders_DirectReferencesDisabled_ClearsList()
        {
            var owner = ScriptableObject.CreateInstance<UIEffectProjectSettings>();
            var registered = new List<Shader> { _first };

            var changed = ShaderVariantRegistry.SynchronizeRegisteredShaders(
                owner, registered, new Shader[0]);

            Assert.That(changed, Is.True);
            Assert.That(registered, Is.Empty);
            Assert.That(EditorUtility.IsDirty(owner), Is.True);
            Object.DestroyImmediate(owner);
        }

        [Test]
        public void SynchronizeRegisteredShaders_EmptyTargetAndList_DoesNotChangeList()
        {
            var registered = new List<Shader>();

            var changed = ShaderVariantRegistry.SynchronizeRegisteredShaders(
                null, registered, new Shader[0]);

            Assert.That(changed, Is.False);
            Assert.That(registered, Is.Empty);
        }

        [Test]
        public void SynchronizeRegisteredShaders_SameGuid_UsesShaderNameAsStableOrder()
        {
            var shaders = new[]
            {
                Shader.Find("Sprites/Default"),
                Shader.Find("Hidden/InternalErrorShader")
            };
            Assert.That(shaders, Has.None.Null);
            var registered = new List<Shader>();

            ShaderVariantRegistry.SynchronizeRegisteredShaders(null, registered, shaders);

            Assert.That(registered.Select(shader => shader.name), Is.Ordered);
        }

        private static string GetStableKey(Shader shader)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(shader));
        }
    }
}
