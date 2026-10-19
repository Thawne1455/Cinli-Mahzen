using System.Collections.Generic;
using CinliMahzen.Core;
using CinliMahzen.Match;
using NUnit.Framework;
using UnityEngine;

namespace CinliMahzen.Tests.EditMode.Match
{
    /// <summary>
    /// TODO 1.3: the pure part of the PawnSpawner — role → prefab kind, NetId = 1 + player slot, spawn point choice.
    /// The MonoBehaviour itself (instantiate / despawn) is verified in play mode.
    /// </summary>
    public class PawnSpawnPlannerTests
    {
        private static readonly Vector3 HumanSpawn = new Vector3(1f, 0f, 2f);
        private static readonly Vector3[] JinnSpawns =
        {
            new Vector3(10f, 1.5f, 0f),
            new Vector3(20f, 1.5f, 0f),
            new Vector3(30f, 1.5f, 0f),
        };

        private static List<PlayerInfo> Players(params Role[] roles)
        {
            var list = new List<PlayerInfo>();
            for (int i = 0; i < roles.Length; i++)
                list.Add(new PlayerInfo { Id = new PlayerId((byte)i), Nickname = "P" + (i + 1), Role = roles[i] });
            return list;
        }

        // --- KindFor / NetIdFor / HasAnyRole ---

        [Test]
        public void KindFor_MapsRolesToPrefabKinds()
        {
            Assert.AreEqual(PawnKind.Human, PawnSpawnPlanner.KindFor(Role.Human));
            Assert.AreEqual(PawnKind.Spirit, PawnSpawnPlanner.KindFor(Role.GoodJinn));
            Assert.AreEqual(PawnKind.Spirit, PawnSpawnPlanner.KindFor(Role.EvilJinn));
            Assert.AreEqual(PawnKind.None, PawnSpawnPlanner.KindFor(Role.None));
            Assert.AreEqual(PawnKind.None, PawnSpawnPlanner.KindFor((Role)42));
        }

        [Test]
        public void NetIdFor_IsOnePlusSlot_WithinThePawnRange()
        {
            Assert.AreEqual(new NetId(1), PawnSpawnPlanner.NetIdFor(0));
            Assert.AreEqual(new NetId(4), PawnSpawnPlanner.NetIdFor(3));
            Assert.AreEqual(new NetId(999), PawnSpawnPlanner.NetIdFor(998));
            Assert.IsFalse(PawnSpawnPlanner.NetIdFor(999).IsValid, "1000 starts the level-object range");
            Assert.IsFalse(PawnSpawnPlanner.NetIdFor(-1).IsValid);
        }

        [Test]
        public void HasAnyRole_IsTrueOnlyOnceRolesAreAssigned()
        {
            Assert.IsFalse(PawnSpawnPlanner.HasAnyRole(null));
            Assert.IsFalse(PawnSpawnPlanner.HasAnyRole(Players(Role.None, Role.None, Role.None, Role.None)));
            Assert.IsTrue(PawnSpawnPlanner.HasAnyRole(Players(Role.None, Role.None, Role.EvilJinn, Role.None)));
        }

        // --- Plan ---

        [Test]
        public void Plan_Round0_OnePawnPerPlayer_WithKindNetIdAndSpawnPoint()
        {
            List<PawnSpawnEntry> plan = PawnSpawnPlanner.Plan(
                Players(Role.Human, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn), HumanSpawn, JinnSpawns);

            Assert.AreEqual(4, plan.Count);

            Assert.AreEqual(PawnKind.Human, plan[0].Kind);
            Assert.AreEqual(Role.Human, plan[0].Role);
            Assert.AreEqual(new PlayerId(0), plan[0].Owner);
            Assert.AreEqual(new NetId(1), plan[0].NetId);
            Assert.AreEqual(HumanSpawn, plan[0].Position);

            Assert.AreEqual(PawnKind.Spirit, plan[1].Kind);
            Assert.AreEqual(Role.GoodJinn, plan[1].Role);
            Assert.AreEqual(new NetId(2), plan[1].NetId);
            Assert.AreEqual(JinnSpawns[0], plan[1].Position);

            Assert.AreEqual(Role.EvilJinn, plan[2].Role);
            Assert.AreEqual(new NetId(3), plan[2].NetId);
            Assert.AreEqual(JinnSpawns[1], plan[2].Position);

            Assert.AreEqual(Role.EvilJinn, plan[3].Role);
            Assert.AreEqual(new PlayerId(3), plan[3].Owner);
            Assert.AreEqual(new NetId(4), plan[3].NetId);
            Assert.AreEqual(JinnSpawns[2], plan[3].Position);

            for (int i = 0; i < plan.Count; i++)
                Assert.AreEqual(i, plan[i].PlayerIndex);
        }

        [Test]
        public void Plan_NetIdFollowsThePlayer_NotTheRole()
        {
            // Round 1 of the rotation: the human is now P1; P0 is an Evil Jinn.
            List<PawnSpawnEntry> plan = PawnSpawnPlanner.Plan(
                Players(Role.EvilJinn, Role.Human, Role.GoodJinn, Role.EvilJinn), HumanSpawn, JinnSpawns);

            Assert.AreEqual(4, plan.Count);
            Assert.AreEqual(new NetId(1), plan[0].NetId);
            Assert.AreEqual(PawnKind.Spirit, plan[0].Kind);
            Assert.AreEqual(new NetId(2), plan[1].NetId);
            Assert.AreEqual(PawnKind.Human, plan[1].Kind);
            Assert.AreEqual(HumanSpawn, plan[1].Position);
            Assert.AreEqual(new NetId(3), plan[2].NetId);
            Assert.AreEqual(new NetId(4), plan[3].NetId);
        }

        [Test]
        public void Plan_JinnsTakeSpawnPointsInPlayerOrder_SkippingTheHuman()
        {
            List<PawnSpawnEntry> plan = PawnSpawnPlanner.Plan(
                Players(Role.EvilJinn, Role.Human, Role.GoodJinn, Role.EvilJinn), HumanSpawn, JinnSpawns);

            Assert.AreEqual(JinnSpawns[0], plan[0].Position, "first jinn in player order");
            Assert.AreEqual(JinnSpawns[1], plan[2].Position, "second jinn");
            Assert.AreEqual(JinnSpawns[2], plan[3].Position, "third jinn");
        }

        [Test]
        public void Plan_MoreJinnsThanSpawnPoints_WrapsAround()
        {
            var twoPoints = new[] { JinnSpawns[0], JinnSpawns[1] };

            List<PawnSpawnEntry> plan = PawnSpawnPlanner.Plan(
                Players(Role.Human, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn), HumanSpawn, twoPoints);

            Assert.AreEqual(twoPoints[0], plan[1].Position);
            Assert.AreEqual(twoPoints[1], plan[2].Position);
            Assert.AreEqual(twoPoints[0], plan[3].Position);
        }

        [Test]
        public void Plan_WithoutJinnSpawnPoints_FallsBackToTheHumanSpawn()
        {
            List<PawnSpawnEntry> none = PawnSpawnPlanner.Plan(
                Players(Role.Human, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn), HumanSpawn, new Vector3[0]);
            List<PawnSpawnEntry> nullList = PawnSpawnPlanner.Plan(
                Players(Role.Human, Role.GoodJinn, Role.EvilJinn, Role.EvilJinn), HumanSpawn, null);

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(HumanSpawn, none[i].Position, "empty list, slot " + i);
                Assert.AreEqual(HumanSpawn, nullList[i].Position, "null list, slot " + i);
            }
        }

        [Test]
        public void Plan_PlayersWithoutARole_GetNoPawn_AndKeepTheirOthersNetIds()
        {
            List<PawnSpawnEntry> plan = PawnSpawnPlanner.Plan(
                Players(Role.None, Role.Human, Role.None, Role.EvilJinn), HumanSpawn, JinnSpawns);

            Assert.AreEqual(2, plan.Count);
            Assert.AreEqual(new NetId(2), plan[0].NetId);
            Assert.AreEqual(PawnKind.Human, plan[0].Kind);
            Assert.AreEqual(new NetId(4), plan[1].NetId);
            Assert.AreEqual(JinnSpawns[0], plan[1].Position, "the first jinn that gets a pawn takes the first point");
        }

        [Test]
        public void Plan_NoRolesAtAll_IsEmpty()
        {
            Assert.AreEqual(0, PawnSpawnPlanner.Plan(Players(Role.None, Role.None, Role.None, Role.None), HumanSpawn, JinnSpawns).Count);
        }

        [Test]
        public void Plan_NullPlayers_IsEmpty()
        {
            Assert.AreEqual(0, PawnSpawnPlanner.Plan(null, HumanSpawn, JinnSpawns).Count);
        }

        [Test]
        public void Plan_SkipsNullPlayerEntries()
        {
            List<PlayerInfo> players = Players(Role.Human, Role.EvilJinn, Role.EvilJinn);
            players[1] = null;

            List<PawnSpawnEntry> plan = PawnSpawnPlanner.Plan(players, HumanSpawn, JinnSpawns);

            Assert.AreEqual(2, plan.Count);
            Assert.AreEqual(new NetId(3), plan[1].NetId);
        }

        [Test]
        public void Plan_NeverLeavesThePawnNetIdRange()
        {
            var roles = new Role[1005];
            for (int i = 0; i < roles.Length; i++)
                roles[i] = Role.EvilJinn;

            List<PawnSpawnEntry> plan = PawnSpawnPlanner.Plan(Players(roles), HumanSpawn, JinnSpawns);

            Assert.AreEqual(999, plan.Count);
            Assert.AreEqual(new NetId(999), plan[plan.Count - 1].NetId);
        }

        [Test]
        public void Plan_FromRoleRotation_Round0And1()
        {
            List<PawnSpawnEntry> round0 = PawnSpawnPlanner.Plan(Players(RoleRotation.CreateRoles(0)), HumanSpawn, JinnSpawns);
            List<PawnSpawnEntry> round1 = PawnSpawnPlanner.Plan(Players(RoleRotation.CreateRoles(1)), HumanSpawn, JinnSpawns);

            Assert.AreEqual(new NetId(1), round0.Find(e => e.Kind == PawnKind.Human).NetId);
            Assert.AreEqual(new NetId(2), round1.Find(e => e.Kind == PawnKind.Human).NetId);
            Assert.AreEqual(1, round0.FindAll(e => e.Kind == PawnKind.Human).Count);
            Assert.AreEqual(3, round1.FindAll(e => e.Kind == PawnKind.Spirit).Count);
        }
    }
}
