using System.Reflection;
using NUnit.Framework;

namespace OrpheusBelow.Tests
{
    // EditMode does not advance frameCount. Restore transient UI locks around isolated combat simulations.
    public abstract class CombatGameplayTestContext
    {
        private object _menu, _frame, _dev, _lesson;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        [SetUp]
        public void EnterCombatTestContext()
        {
            var menu = typeof(UnderworldFrontEnd).GetField("_menuBlocksGameplay", Static);
            var frame = typeof(UnderworldFrontEnd).GetField("_blockThroughFrame", Static);
            var dev = typeof(HadesChamberDirector).GetProperty("IsDevMenuOpen");
            var lesson = typeof(OrpheusHud).GetProperty("IsTutorialModalActive");
            _menu = menu.GetValue(null); _frame = frame.GetValue(null); _dev = dev.GetValue(null); _lesson = lesson.GetValue(null);
            menu.SetValue(null, false); frame.SetValue(null, -1); dev.SetValue(null, false); lesson.SetValue(null, false);
        }
        [TearDown]
        public void RestoreCombatTestContext()
        {
            typeof(UnderworldFrontEnd).GetField("_menuBlocksGameplay", Static).SetValue(null, _menu);
            typeof(UnderworldFrontEnd).GetField("_blockThroughFrame", Static).SetValue(null, _frame);
            typeof(HadesChamberDirector).GetProperty("IsDevMenuOpen").SetValue(null, _dev);
            typeof(OrpheusHud).GetProperty("IsTutorialModalActive").SetValue(null, _lesson);
        }
    }
}
