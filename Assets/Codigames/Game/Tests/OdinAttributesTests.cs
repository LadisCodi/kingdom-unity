using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Codigames.Game.Data;
using NUnit.Framework;
using Sirenix.OdinInspector;

namespace Codigames.Game.Tests
{
    // Odin reports a wrong attribute inside the inspector, not in the console; this catches it before anyone
    // opens the window.
    public class OdinAttributesTests
    {
        private const BindingFlags MEMBERS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [Test]
        public void ListElementLabelName_ShouldNameAMemberOfTheElement()
        {
            var problems = new List<string>();

            foreach (var type in typeof(DefinitionAsset).Assembly.GetTypes())
            {
                foreach (var field in type.GetFields(MEMBERS))
                {
                    var settings = field.GetCustomAttribute<ListDrawerSettingsAttribute>();
                    if (settings == null || settings.ListElementLabelName == null) continue;

                    var element = ElementType(field.FieldType);
                    if (element == null || element.GetMember(settings.ListElementLabelName, MEMBERS).Length == 0)
                        problems.Add($"{type.Name}.{field.Name}: no member \"{settings.ListElementLabelName}\" on {element?.Name ?? "its elements"}");
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        private static Type ElementType(Type list)
            => list.IsArray ? list.GetElementType()
                : list.GetInterfaces().Concat(new[] { list })
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    ?.GetGenericArguments()[0];
    }
}
