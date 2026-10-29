using System;
using System.IO;
using UnityEngine;

namespace OrpheusBelow.EditorTools
{
    // Every coordinate is traced in the unchanged 1536 x 1024 source painting, from its top-left.
    // A passage sits on its painted threshold; arrivals sit farther inside the same physical floor.
    public static class DescentChamberCatalog
    {
        private static Vector2[] P(params float[] xy)
        {
            var points = new Vector2[xy.Length / 2];
            for (int n = 0; n < points.Length; n++) points[n] = new Vector2(xy[n * 2], xy[n * 2 + 1]);
            return points;
        }

        private static PaintedChamberDefinition.Cover Cover(string name, Vector2[] foot, Vector2[] silhouette, float height = 4f)
            => new PaintedChamberDefinition.Cover { name = name, footprint = foot, silhouette = silhouette, height = height };

        private static PaintedChamberDefinition.Cover Pillar(string name, float x, float y, float width, float top)
            => Cover(name, P(x-width,y-width*.35f,x,y-width*.7f,x+width,y-width*.35f,x,y+width*.2f),
                P(x-width*.7f,top,x+width*.7f,top,x+width*.6f,y-width,
                    x+width,y-width*.7f,x+width,y-width*.3f,x,y+width*.2f,
                    x-width,y-width*.3f,x-width,y-width*.7f,x-width*.6f,y-width));

        private static PaintedChamberDefinition.Cover Cypress(string name, float x, float y, float width, float top)
            => Cover(name, P(x-width*.7f,y-14,x,y-30,x+width*.7f,y-14,x+width*.55f,y+8,x-width*.45f,y+8),
                P(x-width*.65f,y,x-width,y-55,x-width*.85f,y-110,x-width*.55f,top+80,
                    x-width*.25f,top+38,x,top,x+width*.2f,top+45,x+width*.55f,top+65,
                    x+width*.58f,y-115,x+width,y-65,x+width*.75f,y-12,x,y+10), 5f);

        public static void Author(PaintedChamberDefinition d, int i)
        {
            if (i < 0 || i >= UnderworldJourney.ChamberCount) throw new ArgumentOutOfRangeException(nameof(i));
            d.index = i;
            d.name = "Chamber_" + i.ToString("D2");
            d.title = UnderworldJourney.ChamberNames[i];
            d.painting = Resources.Load<Texture2D>("PaintedChambers/Chamber_" + i.ToString("D2"));
            if (d.painting == null) throw new FileNotFoundException("Missing supplied chamber painting " + i);
            d.imageWidth = 72f; d.imageHeight = 48f; d.zoom = 11f;
            d.hasGuardian = ChamberEncounterPlan.IsMajorBoss(i);
            d.guardian = i == 1 ? BossIdentity.Tripod : i == 2 ? BossIdentity.MaskedMonk
                : i == 3 ? BossIdentity.MysticEyeWarden : i == 4 ? BossIdentity.WingedSentinel
                : i == 5 ? BossIdentity.Shepherd : BossIdentity.CerberusHoundOfHades;
            AuthorDominantLight(d, i);
            switch (i)
            {
                case 0:
                    d.sourceFilename = "WhatsApp Image 2026-10-27 at 18.57.55 (6).jpeg";
                    d.walkableBoundary = P(215,716,310,664,454,612,601,554,751,493,781,435,
                        846,366,802,324,732,294,843,249,958,211,1070,168,1160,167,
                        1230,195,1306,199,1391,247,1464,293,1502,326,1462,366,
                        1310,321,1200,299,1123,328,1050,369,1008,421,994,465,
                        946,525,891,566,787,608,663,659,557,709,438,754,333,799,237,775);
                    d.returnExit = new Vector2(1268,258); d.entrance = new Vector2(1132,291);
                    d.exit = new Vector2(275,733); d.returnSpawn = new Vector2(418,682);
                    d.well = new Vector2(1240,284); d.encounterCenter = new Vector2(760,553);
                    d.enemySpawns = P(566,630,672,588,798,548,882,475,953,392,1044,310,1295,285);
                    d.cover = new[] {
                        Pillar("LowerArchLeftPier",158,728,47,412), Pillar("LowerArchRightPier",359,817,52,523),
                        Cypress("BridgeWestCypress",477,620,34,360), Cypress("BridgeRearCypress",736,515,30,303),
                        Cypress("BridgeFrontCypress",954,577,36,413), Cypress("UpperLandingCypress",1105,408,35,206),
                        Cypress("UpperWestCypress",938,323,30,118),
                        Pillar("TempleLeftColumn",1186,263,25,61), Pillar("TempleRightColumn",1353,286,24,82),
                        Cover("BridgeFlowerUrn",P(704,630,723,618,747,629,731,644),
                            P(702,614,710,599,728,601,747,613,747,629,731,644,711,637),1.3f),
                        Cover("WesternBridgeBrazier",P(535,558,550,549,565,560,551,574),
                            P(532,516,558,511,568,525,558,539,565,560,551,574,537,562),2f)
                    };
                    break;
                case 1:
                    d.sourceFilename = "WhatsApp Image 2026-10-27 at 18.57.55 (5).jpeg";
                    d.walkableBoundary = P(204,435,291,396,376,414,439,423,534,389,587,362,
                        714,389,787,361,863,323,938,295,1016,309,1110,339,1180,376,
                        1205,418,1280,468,1352,544,1430,623,1398,666,1272,642,1157,625,
                        1077,650,1027,679,922,707,803,746,685,778,565,790,448,778,
                        329,738,245,681,223,591,260,532,233,482);
                    d.returnExit = new Vector2(1007,342); d.entrance = new Vector2(1019,445);
                    d.exit = new Vector2(265,445); d.returnSpawn = new Vector2(408,491);
                    d.well = new Vector2(935,455); d.encounterCenter = new Vector2(830,575);
                    d.enemySpawns = P(498,526,676,561,821,516,1031,528,1099,586,908,639,657,683);
                    d.cover = new[] {
                        Pillar("GardenArchLeftPier",124,495,44,192), Pillar("GardenArchRightPier",356,432,40,143),
                        Pillar("RearGardenColumn",608,489,35,232), Pillar("EasternGardenColumn",876,453,34,295),
                        Cypress("InnerGardenCypress",749,469,39,215), Cypress("UpperGardenCypress",1101,423,31,268),
                        Pillar("EastBrokenColumn",1151,558,35,448), Pillar("FrontGardenColumn",542,770,43,577),
                        Pillar("WestBrokenColumn",395,727,34,611), Pillar("EasternBalustradePier",1287,672,44,533),
                        Cypress("ForegroundGardenCypress",187,803,58,370),
                        Cover("CentralFlowerBed",P(734,665,767,641,807,651,831,674,801,691,754,687),
                            P(723,657,737,630,766,613,782,579,795,615,827,636,835,669,801,691,754,687),1.8f)
                    };
                    break;
                case 2:
                    d.sourceFilename = "WhatsApp Image 2026-10-27 at 18.57.55 (1).jpeg";
                    d.walkableBoundary = P(397,715,439,623,350,589,259,547,244,498,312,455,
                        424,410,527,361,609,342,732,363,899,327,1029,278,1125,242,
                        1192,256,1267,223,1323,259,1272,330,1183,378,1100,424,
                        1182,507,1270,541,1350,600,1385,674,1316,727,1186,779,
                        1060,836,951,898,872,936,742,909,650,899,544,853,473,803);
                    d.returnExit = new Vector2(590,824); d.entrance = new Vector2(729,780);
                    d.exit = new Vector2(1250,281); d.returnSpawn = new Vector2(1120,354);
                    d.well = new Vector2(776,820); d.encounterCenter = new Vector2(1004,663);
                    d.enemySpawns = P(531,553,684,452,786,502,1007,600,1155,628,1093,741,856,793);
                    d.cover = new[] {
                        Cypress("CentralGreatCypress",668,674,56,288), Cypress("RearGreatCypress",711,363,44,0),
                        Cypress("WestGardenCypress",408,621,30,434), Cypress("EastGardenCypress",1184,602,42,327),
                        Cypress("WesternGreatCypress",333,432,42,0), Cypress("InnerGoldenCypress",823,702,28,521),
                        Pillar("WestArcadeColumn",195,550,40,321), Pillar("WestInnerColumn",422,437,36,243),
                        Pillar("GardenBrokenColumn",549,525,30,419), Pillar("RearGardenColumn",819,401,39,178),
                        Pillar("MiddleGardenColumn",913,476,40,276), Pillar("FrontBrokenColumn",744,916,32,830),
                        Pillar("GoldenGateLeftPier",1178,267,28,32), Pillar("GoldenGateRightPier",1369,306,30,49),
                        Cover("FallenGardenLintel",P(949,527,1000,503,1078,538,1052,560,1003,550),
                            P(949,502,990,472,1048,484,1095,515,1078,538,1052,560,1003,550),2.2f),
                        Cover("GardenMarbleBench",P(898,718,929,698,971,710,957,735,924,741),
                            P(895,696,925,680,969,690,983,707,971,710,957,735,924,741),1.6f)
                    };
                    break;
                case 3:
                    d.sourceFilename = "WhatsApp Image 2026-10-27 at 18.57.54.jpeg";
                    d.walkableBoundary = P(219,389,344,364,435,403,554,358,655,316,773,267,
                        904,264,1037,281,1179,337,1280,387,1358,442,1401,512,
                        1420,598,1502,658,1510,738,1404,810,1280,870,1184,862,
                        1041,823,921,786,796,747,662,710,554,668,459,618,368,570,254,496);
                    d.returnExit = new Vector2(1360,783); d.entrance = new Vector2(1220,739);
                    d.exit = new Vector2(271,394); d.returnSpawn = new Vector2(422,448);
                    d.well = new Vector2(1255,657); d.encounterCenter = new Vector2(984,564);
                    d.enemySpawns = P(467,491,628,517,814,446,996,421,1176,472,1240,583,952,686);
                    d.cover = new[] {
                        Pillar("WitheredArchLeftPier",156,468,40,135), Pillar("WitheredArchRightPier",350,381,37,59),
                        Pillar("WestBrokenPillar",536,397,32,284), Pillar("CourtCenterPillar",702,467,35,337),
                        Pillar("RearCourtPillar",842,389,34,248), Pillar("ArcadeEastPillar",1256,392,35,232),
                        Cypress("RearWitheredCypress",784,373,37,170), Cypress("WestWitheredCypress",408,371,38,119),
                        Pillar("FrontCourtColumn",525,731,40,549), Pillar("FrontEasternColumn",1114,722,35,569),
                        Cover("WesternFallenColumn",P(555,463,586,436,650,473,642,498,610,503),
                            P(548,425,574,401,609,412,650,455,650,473,642,498,610,503),2f),
                        Cover("CentralFallenLintel",P(748,544,782,517,885,556,877,584,839,593),
                            P(743,506,774,475,814,482,900,537,885,556,877,584,839,593),2.1f),
                        Cover("FrontDeadTree",P(667,757,697,739,725,754,711,777,680,779),
                            P(687,748,674,700,646,665,631,649,642,652,671,679,665,638,674,655,686,695,
                                700,718,711,682,734,662,736,616,744,629,747,652,782,651,758,666,731,685,717,739,725,754,711,777,680,779),4f),
                        Cover("CourtBrokenPedestal",P(1042,503,1063,484,1091,494,1105,521,1077,536,1050,525),
                            P(1038,455,1061,430,1093,435,1105,453,1096,477,1105,521,1077,536,1050,525),2.4f)
                    };
                    break;
                case 4:
                    d.sourceFilename = "WhatsApp Image 2026-10-27 at 18.57.55 (2).jpeg";
                    d.walkableBoundary = P(205,414,271,390,369,405,470,398,566,421,665,443,
                        671,538,754,591,806,638,900,673,960,715,1060,754,1160,739,1210,696,
                        1070,663,1045,630,996,580,865,552,812,506,866,461,1010,401,1165,391,
                        1290,441,1385,515,1430,570,1390,650,1330,697,1317,778,1440,815,
                        1420,909,1312,942,1180,914,1058,859,961,831,865,809,748,802,
                        645,756,612,679,543,652,448,621,351,575,260,541,217,463);
                    d.returnExit = new Vector2(1372,847); d.entrance = new Vector2(1240,850);
                    d.exit = new Vector2(280,437); d.returnSpawn = new Vector2(430,476);
                    d.well = new Vector2(1166,814); d.encounterCenter = new Vector2(1175,540);
                    d.enemySpawns = P(408,515,542,550,720,707,960,799,1181,858,1165,584,1269,553);
                    d.cover = new[] {
                        Pillar("LavaArchLeftPier",133,412,44,116), Pillar("LavaArchRightPier",338,418,38,87),
                        Pillar("WesternTerraceColumn",270,544,37,346), Pillar("LowerWesternPillar",619,547,35,405),
                        Pillar("BrokenCausewayPillar",645,770,34,648), Pillar("MiddleCausewayPillar",876,676,39,514),
                        Pillar("EastArenaColumn",1307,514,42,340), Pillar("RearArenaColumn",975,373,38,154),
                        Pillar("ArrivalBridgePillar",1267,938,43,711), Pillar("ArrivalArchPier",1456,846,40,550),
                        Cover("LowerFallenLintel",P(783,730,819,710,929,753,916,777,867,781),
                            P(779,700,809,671,854,688,941,738,929,753,916,777,867,781),2.4f),
                        Cover("WestFallenLintel",P(462,402,490,383,565,416,556,443,524,445),
                            P(451,370,481,344,516,360,565,397,565,416,556,443,524,445),2.2f),
                        Cover("RearBasaltCrag",P(1065,375,1101,348,1135,372,1143,405,1100,412),
                            P(1064,375,1080,335,1101,333,1117,347,1135,372,1143,405,1100,412),2.5f)
                    };
                    break;
                case 5:
                    d.sourceFilename = "WhatsApp Image 2026-10-27 at 18.57.55 (3).jpeg";
                    d.walkableBoundary = P(54,363,131,330,229,310,332,323,426,339,531,349,575,377,
                        536,418,468,440,439,469,500,504,575,532,660,557,738,586,817,624,
                        915,659,1023,704,1124,745,1240,763,1349,792,1435,845,1457,906,
                        1371,944,1244,926,1143,901,1042,867,949,844,850,819,745,790,
                        647,754,555,716,468,678,381,638,299,596,219,560,145,524,108,480,110,433,62,407);
                    d.returnExit = new Vector2(1384,885); d.entrance = new Vector2(1243,844);
                    d.exit = new Vector2(105,381); d.returnSpawn = new Vector2(297,398);
                    d.well = new Vector2(1190,801); d.encounterCenter = new Vector2(608,651);
                    d.enemySpawns = P(342,399,355,526,512,601,675,673,799,715,1003,780,1158,829);
                    d.cover = new[] {
                        Pillar("FallsArchLeftPier",38,390,35,159), Pillar("FallsArchRightPier",155,393,34,193),
                        Pillar("WestBrazierPillar",231,424,30,268), Pillar("UpperBrokenPier",530,512,28,435),
                        Pillar("UpperFallsArchLeft",369,321,30,167), Pillar("UpperFallsArchRight",489,345,27,209),
                        Pillar("ArenaWestColumn",440,704,39,552), Pillar("ArenaEastColumn",854,642,34,489),
                        Pillar("MidFallsArchLeft",649,570,28,450), Pillar("MidFallsArchRight",709,610,25,479),
                        Pillar("LowerFallsColumn",879,912,36,754), Pillar("ArrivalBrazier",1035,941,35,850),
                        Pillar("ArrivalArchLeftPier",1320,900,38,637), Pillar("ArrivalArchRightPier",1462,938,42,665),
                        Cover("FallsFallenBalustrade",P(341,462,369,441,459,476,448,501,415,503),
                            P(337,437,367,413,403,424,466,460,459,476,448,501,415,503),1.8f),
                        Cover("FallsArenaRubble",P(750,639,789,618,830,639,817,671,780,678),
                            P(747,617,771,592,798,601,830,639,817,671,780,678),2f),
                        Cover("FallsBasaltTooth",P(1024,750,1052,730,1087,746,1091,773,1053,786),
                            P(1018,748,1027,702,1040,678,1057,683,1065,717,1087,746,1091,773,1053,786),3f)
                    };
                    break;
                case 6:
                    d.sourceFilename = "WhatsApp Image 2026-10-27 at 18.57.55.jpeg";
                    d.walkableBoundary = P(118,387,196,349,289,342,380,371,449,418,542,457,
                        648,491,740,500,827,493,916,493,1014,519,1119,554,1230,578,
                        1320,613,1418,650,1467,702,1450,764,1373,791,1266,810,
                        1151,823,1033,812,917,792,806,768,695,734,599,696,509,651,
                        434,602,354,572,271,537,185,501,126,459);
                    d.returnExit = new Vector2(215,407); d.entrance = new Vector2(365,451);
                    d.exit = new Vector2(1384,726); d.returnSpawn = new Vector2(1240,711);
                    d.well = new Vector2(414,482); d.encounterCenter = new Vector2(1000,670);
                    d.enemySpawns = P(368,485,518,567,735,716,887,700,1031,740,1198,678,1268,743);
                    d.cover = new[] {
                        Pillar("TitanArrivalArchLeft",135,405,38,78), Pillar("TitanArrivalArchRight",371,404,37,130),
                        Pillar("TitanRearColumn",640,434,34,202), Pillar("TitanHandColumn",777,525,35,363),
                        Pillar("TitanForecourtColumn",391,800,40,573), Pillar("TitanFarRightColumn",1479,584,41,315),
                        Pillar("TitanDepartureArchLeft",1366,799,40,637), Pillar("TitanDepartureArchRight",1502,751,31,536),
                        // Separate convex footprints match the static convex collision meshes without filling the arena.
                        Cover("FallenTitanArmAndHand",P(431,451,470,427,693,526,706,550,687,574,655,566,584,542,545,524,465,480),
                            P(438,281,483,239,522,264,539,306,554,344,571,379,583,412,619,436,651,453,
                                671,474,686,502,704,526,713,550,700,570,686,574,677,543,665,523,658,558,
                                647,568,633,551,628,523,617,520,614,550,600,557,584,542,569,515,544,507,
                                513,489,479,463,454,429,438,386,425,339),5f),
                        Cover("FallenTitanTorso",P(543,351,615,320,704,348,787,399,839,440,857,478,829,508,771,511,711,489,588,416),
                            P(519,163,578,118,649,89,711,77,774,106,814,156,840,198,889,197,936,175,
                                994,148,1028,106,1060,92,1102,118,1133,168,1158,197,1145,224,1118,222,
                                1081,196,1070,228,1052,267,1041,306,1012,329,985,336,966,372,936,406,
                                900,429,865,450,857,478,829,508,771,511,711,489,669,460,630,437,588,416,
                                549,359,522,308,507,258),8f),
                        Cover("FallenTitanRightLeg",P(823,457,860,434,923,452,989,473,1187,557,1231,576,
                                1219,603,1185,603,953,543,868,496),
                            P(833,351,888,321,948,331,990,360,1030,381,1080,412,1120,448,1162,468,
                                1210,501,1241,540,1244,571,1217,600,1184,596,1156,574,1138,551,1092,531,
                                1052,512,1029,529,994,552,953,543,912,518,868,496,843,439),6f),
                        Cover("TitanForecourtRubble",P(658,629,689,600,735,605,788,636,779,662,744,673,690,653),
                            P(645,588,665,553,697,552,724,574,748,583,783,605,807,630,788,636,
                                779,662,744,673,690,653),2.8f),
                        Cover("TitanBrokenFoot",P(1023,888,1079,860,1133,884,1177,924,1168,955,1130,974,1075,956),
                            P(971,790,1017,779,1083,809,1130,844,1174,881,1190,922,1168,955,1130,974,
                                1075,956,1032,921,1004,865),3.5f)
                    };
                    break;
                case 7:
                    d.sourceFilename = "WhatsApp Image 2026-10-27 at 18.57.55 (4).jpeg";
                    d.walkableBoundary = P(253,814,305,774,378,737,450,722,521,687,578,633,654,590,
                        743,537,836,489,906,448,974,407,1006,365,1027,322,1096,282,
                        1167,248,1238,242,1300,262,1370,297,1440,325,1498,354,
                        1481,414,1410,448,1320,486,1237,525,1158,569,1080,614,1006,662,
                        // The lower landing ends at its parapet. The diagonal stone wall beyond
                        // (964,715) is foreground architecture, not another playable platform.
                        964,715,912,749,853,795,782,763,697,727,616,724,543,745,463,787,384,837,312,869);
                    // Arrive beyond the tall foreground jamb, with the whole actor visible on carpet.
                    d.returnExit = new Vector2(327,802); d.entrance = new Vector2(810,610);
                    // The rescue happens on the carpet in front of the solid throne, never inside it.
                    d.exit = new Vector2(1171,351); d.returnSpawn = new Vector2(1100,388);
                    d.well = new Vector2(710,605); d.encounterCenter = new Vector2(942,533);
                    d.enemySpawns = P(760,625,855,595,975,540,1081,473,1185,426,1322,373,1086,339);
                    d.cover = new[] {
                        Pillar("ThroneArrivalLeftPier",261,827,45,454), Pillar("ThroneRearWallColumn",523,517,43,367),
                        // The tall foreground jamb is separate from the rear wall column at (523,517).
                        // Its base is below the arrival floor; its crown/banner must occlude actors behind the arch.
                        Cover("ThroneForegroundGateFrame",P(416,942,474,910,552,945,564,975,503,1006,440,983),
                            P(214,526,244,495,287,457,335,424,383,410,421,415,462,442,507,483,
                                548,519,576,552,568,594,566,940,548,980,502,1006,440,984,418,946,
                                433,899,435,607,413,571,390,538,363,515,341,516,316,538,289,574,
                                271,611,263,650,246,638,248,599,258,561),7f),
                        Pillar("CarpetWestBrazier",739,489,31,403), Pillar("CarpetRearBrazier",917,420,34,314),
                        // This pier stands in front of the first stair. Its base is at y754;
                        // tracing only the shaft around y642 lets actors stand on the stone plinth.
                        Cover("CarpetFrontColumn",P(601,697,646,671,706,696,713,726,672,754,615,728),
                            P(620,529,643,523,672,536,680,552,675,576,681,591,698,607,696,652,
                                714,688,713,726,672,754,615,728,602,700,615,680,613,636,
                                600,619,601,599,619,593,622,574,609,560,610,544),5f),
                        Pillar("CarpetEastColumn",940,731,33,617),
                        Pillar("CarpetEastBrazier",1192,631,37,537), Pillar("StairEastBrazier",1291,472,33,379),
                        Pillar("ThroneWestMonument",806,409,48,181), Pillar("ThroneEastMonument",1432,383,44,177),
                        Pillar("ThroneRearBrazier",1044,287,26,216), Pillar("ThroneRightBrazier",1345,328,27,271),
                        Cover("HadesThrone",P(1164,267,1204,244,1261,258,1290,289,1266,314,1223,321,1183,302),
                            P(1167,227,1190,199,1196,166,1210,173,1222,141,1235,152,1246,112,
                                1260,134,1271,169,1287,162,1299,187,1284,214,1290,243,1303,260,
                                1290,289,1266,314,1223,321,1183,302,1172,279),5f)
                    };
                    break;
            }
            d.reunionPoint = d.exit;
            AuthorStairs(d, i);
        }

        private static void AuthorDominantLight(PaintedChamberDefinition d, int i)
        {
            switch (i)
            {
                case 0: d.dominantLightDirection = new Vector2(.82f, .57f); d.dominantLightColor = new Color(1f, .72f, .34f, 1f); d.dominantLightIntensity = 1.15f; break;
                case 1: d.dominantLightDirection = new Vector2(-.72f, .69f); d.dominantLightColor = new Color(.96f, .48f, .24f, 1f); d.dominantLightIntensity = 1.28f; break;
                case 2: d.dominantLightDirection = new Vector2(.34f, .94f); d.dominantLightColor = new Color(.62f, .72f, 1f, 1f); d.dominantLightIntensity = .82f; break;
                case 3: d.dominantLightDirection = new Vector2(-.93f, .36f); d.dominantLightColor = new Color(.88f, .38f, 1f, 1f); d.dominantLightIntensity = 1.05f; break;
                case 4: d.dominantLightDirection = new Vector2(.96f, -.28f); d.dominantLightColor = new Color(1f, .36f, .18f, 1f); d.dominantLightIntensity = 1.42f; break;
                case 5: d.dominantLightDirection = new Vector2(-.45f, -.89f); d.dominantLightColor = new Color(.54f, .82f, .92f, 1f); d.dominantLightIntensity = .76f; break;
                case 6: d.dominantLightDirection = new Vector2(.67f, -.74f); d.dominantLightColor = new Color(1f, .53f, .22f, 1f); d.dominantLightIntensity = 1.34f; break;
                default: d.dominantLightDirection = new Vector2(-.83f, -.56f); d.dominantLightColor = new Color(.92f, .24f, .16f, 1f); d.dominantLightIntensity = 1.55f; break;
            }
        }

        public static void AuthorStairs(PaintedChamberDefinition d, int i)
        {
            PaintedChamberDefinition.StairZone Stair(string name, Vector2[] polygon, float dx, float dy)
                => new PaintedChamberDefinition.StairZone { name = name, polygon = polygon, uphill = new Vector2(dx,dy) };
            switch (i)
            {
                case 0:
                    d.stairs = new[] {
                        Stair("SanctuaryGrandStair",P(770,516,841,425,1004,464,933,554),75,-95),
                        Stair("TempleApproachStair",P(956,366,1046,310,1166,342,1074,403),95,-60),
                        Stair("TempleRoundSteps",P(1148,290,1241,247,1375,285,1301,323),70,-60)
                    }; break;
                case 1:
                    d.stairs = new[] {
                        Stair("GardenEastSteps",P(932,410,1045,353,1152,389,1046,451),100,-60),
                        Stair("GardenWestThreshold",P(213,476,295,434,357,452,277,504),-80,-45)
                    }; break;
                case 2:
                    d.stairs = new[] {
                        Stair("GoldenGateSteps",P(1085,366,1191,301,1299,334,1191,396),100,-65),
                        Stair("GardenLowerStair",P(484,821,603,750,710,785,594,858),115,-70)
                    }; break;
                case 3:
                    d.stairs = new[] {
                        Stair("WitheredLowerTerrace",P(850,768,989,690,1117,729,978,813),135,-80),
                        Stair("WitheredWestThreshold",P(222,444,294,400,369,420,298,464),-70,-45)
                    }; break;
                case 4:
                    d.stairs = new[] {
                        Stair("CausewayLowerSteps",P(670,738,767,682,863,724,768,780),95,-60),
                        Stair("EasternArenaSteps",P(1161,745,1241,680,1326,710,1240,777),85,-65),
                        Stair("LavaGateThreshold",P(243,472,316,427,391,448,314,498),-75,-45)
                    }; break;
                case 5:
                    d.stairs = new[] {
                        Stair("FallsArenaStairs",P(292,547,394,483,488,518,391,584),100,-65),
                        Stair("FallsLowerApproach",P(1134,877,1233,814,1344,853,1242,915),-100,-60)
                    }; break;
                case 6:
                    d.stairs = new[] {
                        Stair("TitanArrivalThreshold",P(155,448,246,397,326,421,237,474),-85,-50),
                        Stair("TitanDepartureSteps",P(1233,756,1326,700,1421,733,1325,791),90,-55)
                    }; break;
                default:
                    d.stairs = new[] {
                        Stair("ThroneLowerCarpetStair",P(657,698,730,643,942,710,869,763),80,-60),
                        Stair("ThroneUpperCarpetStair",P(927,450,1001,396,1233,466,1163,513),80,-60),
                        Stair("ThroneArrivalThreshold",P(266,846,350,799,420,821,337,870),-85,-50)
                    }; break;
            }
        }
    }
}
