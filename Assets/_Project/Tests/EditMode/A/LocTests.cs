using CinliMahzen.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CinliMahzen.Tests.EditMode.A
{
    public class LocTests
    {
        private LocTable _a;
        private LocTable _b;

        [SetUp]
        public void SetUp()
        {
            _a = ScriptableObject.CreateInstance<LocTable>();
            _a.name = "LocTable_TestA";
            _a.Set("role.human", "İnsan", "Human");
            _a.Set("only.tr", "Sadece", "");
            _b = ScriptableObject.CreateInstance<LocTable>();
            _b.name = "LocTable_TestB";
            _b.Set("poss.shelf", "Raf", "Shelf");
            Loc.UseTables(_a, _b);
            Loc.Language = LocLanguage.Tr;
        }

        [TearDown]
        public void TearDown()
        {
            Loc.Clear();
            Loc.Language = LocLanguage.Tr;
            Object.DestroyImmediate(_a);
            Object.DestroyImmediate(_b);
        }

        [Test]
        public void MultipleTables_AreMerged()
        {
            Assert.AreEqual("İnsan", Loc.T("role.human"));
            Assert.AreEqual("Raf", Loc.T("poss.shelf"));
        }

        [Test]
        public void English_FallsBackToTurkishWhenEmpty()
        {
            Loc.Language = LocLanguage.En;
            Assert.AreEqual("Human", Loc.T("role.human"));
            Assert.AreEqual("Sadece", Loc.T("only.tr"));
        }

        [Test]
        public void MissingKey_ReturnsHashKey_AndWarnsOnce()
        {
            LogAssert.Expect(LogType.Warning, "[Loc] Missing key: nope");
            Assert.AreEqual("#nope", Loc.T("nope"));
            Assert.AreEqual("#nope", Loc.T("nope"));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void DuplicateKey_FirstWins_AndWarns()
        {
            var dup = ScriptableObject.CreateInstance<LocTable>();
            dup.name = "LocTable_Dup";
            dup.Set("role.human", "X", "X");
            LogAssert.Expect(LogType.Warning, "[Loc] Duplicate key 'role.human' in LocTable_Dup (first one wins)");
            Loc.AddTable(dup);
            Assert.AreEqual("İnsan", Loc.T("role.human"));
            Object.DestroyImmediate(dup);
        }

        [Test]
        public void Format_WithArgs()
        {
            _a.Set("hud.round", "Raund {0}", "Round {0}");
            Loc.UseTables(_a);
            Assert.AreEqual("Raund 2", Loc.T("hud.round", 2));
        }
    }
}
