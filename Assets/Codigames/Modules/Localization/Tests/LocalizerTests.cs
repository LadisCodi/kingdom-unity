using System.Collections.Generic;
using Codigames.Modules.Localization;
using NUnit.Framework;

namespace Codigames.Modules.Localization.Tests
{
    public class LocalizerTests
    {
        private Localizer _localizer;

        [SetUp]
        public void SetUp()
        {
            _localizer = new Localizer();
            _localizer.AddTranslations("es", new Dictionary<string, string>
            {
                ["Build"] = "Construir",
                ["noun::Build"] = "Construcción",
                ["A tap takes +{n}%"] = "Un toque recoge +{n}%",
                ["{n} coin|{n} coins"] = "{n} moneda|{n} monedas",
            });
        }

        [Test]
        public void Tr_ShouldReturnTheSourceInTheSourceLanguage()
        {
            Assert.That(_localizer.Tr("Build"), Is.EqualTo("Build"));
            Assert.That(_localizer.Tr("noun::Build"), Is.EqualTo("Build"));
        }

        [Test]
        public void Tr_ShouldTranslateByContextInSpanish()
        {
            _localizer.SetCulture("es-ES");

            Assert.That(_localizer.Tr("Build"), Is.EqualTo("Construir"));
            Assert.That(_localizer.Tr("noun::Build"), Is.EqualTo("Construcción"));
        }

        [Test]
        public void Tr_ShouldFallBackToTheSourceForAnUntranslatedLine()
        {
            _localizer.SetCulture("es-ES");

            Assert.That(_localizer.Tr("Collect"), Is.EqualTo("Collect"));
        }

        [Test]
        public void Tr_ShouldFillPlaceholders()
        {
            _localizer.SetCulture("es-ES");

            Assert.That(_localizer.Tr("A tap takes +{n}%", ("n", 15)), Is.EqualTo("Un toque recoge +15%"));
        }

        [Test]
        public void Trn_ShouldPickTheFormByCount()
        {
            Assert.That(_localizer.Trn(1, "{n} coin", "{n} coins", ("n", 1)), Is.EqualTo("1 coin"));

            _localizer.SetCulture("es-ES");

            Assert.That(_localizer.Trn(3, "{n} coin", "{n} coins", ("n", 3)), Is.EqualTo("3 monedas"));
        }
    }
}
