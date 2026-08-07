using System;
using System.Collections.Generic;
using System.IO;
using Bejeweled3Accessible.Engine;
using Bejeweled3Accessible.Audio;
using Bejeweled3Accessible.Accessibility;

namespace Bejeweled3Accessible.Tests
{
    public static class Assert
    {
        public static void True(bool condition, string message)
        {
            if (!condition) throw new Exception("ASSERT: " + message);
        }

        public static void False(bool condition, string message)
        {
            True(!condition, message);
        }

        public static void Equal(object expected, object actual, string message)
        {
            True(object.Equals(expected, actual), string.Format("{0} (esperado: {1}, actual: {2})", message, expected, actual));
        }

        public static void NotNull(object value, string message)
        {
            True(value != null, message);
        }

        public static void Near(float expected, float actual, float tolerance, string message)
        {
            True(Math.Abs(expected - actual) <= tolerance, string.Format("{0} (esperado: {1}, actual: {2})", message, expected, actual));
        }

        public static void NoThrow(Action action, string message)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                throw new Exception(message + " -> " + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }

    public class TestRunner
    {
        public static int Main(string[] args)
        {
            if (args != null && args.Length > 0 && args[0] == "--hrtf-scan")
            {
                RunHrtfScan();
                return 0;
            }

            Console.WriteLine("=== SUITE DE TESTS UNITARIOS - BEJEWELED 3 ACCESIBLE ===");
            List<Tuple<string, Action>> tests = BuildTestList();
            int passed = 0;
            int failed = 0;

            foreach (var test in tests)
            {
                try
                {
                    test.Item2();
                    passed++;
                    Console.WriteLine("[EXITO] " + test.Item1);
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine("[FALLO] " + test.Item1 + " -> " + ex.Message);
                }
            }

            Console.WriteLine();
            Console.WriteLine(string.Format("RESULTADO: {0} de {1} tests pasaron ({2} fallos).", passed, tests.Count, failed));
            return failed == 0 ? 0 : 1;
        }

        private static List<Tuple<string, Action>> BuildTestList()
        {
            var tests = new List<Tuple<string, Action>>();

            // ======================= BOARD =======================
            tests.Add(Tuple.Create<string, Action>("Board: inicializacion completa y colores validos", () =>
            {
                Board b = new Board(12345);
                var colors = Enum.GetValues(typeof(GemColor));
                for (int y = 0; y < Board.Rows; y++)
                    for (int x = 0; x < Board.Cols; x++)
                    {
                        Gem g = b.GetGem(x, y);
                        Assert.NotNull(g, "Celda " + x + "," + y + " debe tener gema");
                        Assert.True(Array.IndexOf(colors, g.Color) >= 0, "Color invalido en " + x + "," + y);
                    }
            }));

            tests.Add(Tuple.Create<string, Action>("Board: sin matches iniciales", () =>
            {
                for (int seed = 1; seed <= 10; seed++)
                {
                    Board b = new Board(seed);
                    Assert.False(b.HasAnyMatch(), "Tablero seed " + seed + " no debe tener matches");
                }
            }));

            tests.Add(Tuple.Create<string, Action>("Board: inicializacion con movimiento valido garantizado", () =>
            {
                for (int seed = 1; seed <= 20; seed++)
                {
                    Board b = new Board(seed);
                    Assert.NotNull(HintFinder.FindValidMove(b), "Seed " + seed + " debe tener al menos un movimiento");
                }
            }));

            tests.Add(Tuple.Create<string, Action>("Board: inicializacion determinista con misma semilla", () =>
            {
                Board b1 = new Board(777);
                Board b2 = new Board(777);
                for (int y = 0; y < Board.Rows; y++)
                    for (int x = 0; x < Board.Cols; x++)
                        Assert.Equal(b1.GetGem(x, y).Color, b2.GetGem(x, y).Color, "Mismatch en " + x + "," + y);
            }));

            tests.Add(Tuple.Create<string, Action>("Board: GetGem fuera de rango devuelve null", () =>
            {
                Board b = new Board(1);
                Assert.True(b.GetGem(-1, 0) == null && b.GetGem(0, -1) == null && b.GetGem(8, 0) == null && b.GetGem(0, 8) == null, "Debe devolver null fuera de rango");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: SetGem/GetGem roundtrip", () =>
            {
                Board b = new Board(1);
                b.SetGem(5, 5, new Gem(GemColor.Orange, SpecialType.Flame));
                Gem g = b.GetGem(5, 5);
                Assert.Equal(GemColor.Orange, g.Color, "Color");
                Assert.Equal(SpecialType.Flame, g.Special, "Tipo especial");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: swap no adyacente rechazado", () =>
            {
                Board b = new Board(2);
                Assert.False(b.SwapGems(0, 0, 3, 3), "Swap diagonal lejano debe fallar");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: swap invalido restaura el tablero", () =>
            {
                Board b = new Board(3);
                FillCheckerboard(b);
                GemColor c00 = b.GetGem(0, 0).Color;
                GemColor c10 = b.GetGem(1, 0).Color;
                Assert.False(b.SwapGems(0, 0, 1, 0), "Swap sin match debe fallar");
                Assert.Equal(c00, b.GetGem(0, 0).Color, "Celda (0,0) debe restaurarse");
                Assert.Equal(c10, b.GetGem(1, 0).Color, "Celda (1,0) debe restaurarse");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: swap valido crea match y confirma", () =>
            {
                Board b = new Board(4);
                FillCheckerboard(b);
                b.SetGem(2, 3, new Gem(GemColor.Red));
                b.SetGem(3, 3, new Gem(GemColor.Red));
                b.SetGem(3, 4, new Gem(GemColor.Red));
                b.SetGem(4, 4, new Gem(GemColor.Red));
                Assert.True(b.SwapGems(2, 3, 2, 4), "Swap (2,3)<->(2,4) debe crear match vertical");
                Assert.Equal(GemColor.Red, b.GetGem(2, 4).Color, "La gema roja debe quedar en (2,4)");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: TestSwap detecta sin mutar", () =>
            {
                Board b = new Board(5);
                FillCheckerboard(b);
                b.SetGem(2, 3, new Gem(GemColor.Red));
                b.SetGem(3, 3, new Gem(GemColor.Red));
                b.SetGem(3, 4, new Gem(GemColor.Red));
                b.SetGem(4, 4, new Gem(GemColor.Red));
                GemColor before1 = b.GetGem(2, 3).Color;
                GemColor before2 = b.GetGem(2, 4).Color;
                Assert.True(b.TestSwap(2, 3, 2, 4), "TestSwap debe detectar el match");
                Assert.Equal(before1, b.GetGem(2, 3).Color, "No debe mutar (2,3)");
                Assert.Equal(before2, b.GetGem(2, 4).Color, "No debe mutar (2,4)");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: HasAnyMatch detecta horizontal", () =>
            {
                Board b = new Board(6);
                FillCheckerboard(b);
                b.SetGem(1, 3, new Gem(GemColor.Red));
                b.SetGem(2, 3, new Gem(GemColor.Red));
                b.SetGem(3, 3, new Gem(GemColor.Red));
                Assert.True(b.HasAnyMatch(), "Debe detectar match horizontal");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: HasAnyMatch detecta vertical", () =>
            {
                Board b = new Board(7);
                FillCheckerboard(b);
                b.SetGem(3, 1, new Gem(GemColor.Blue));
                b.SetGem(3, 2, new Gem(GemColor.Blue));
                b.SetGem(3, 3, new Gem(GemColor.Blue));
                Assert.True(b.HasAnyMatch(), "Debe detectar match vertical");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: ProcessMatches destruye match-3", () =>
            {
                Board b = new Board(8);
                FillCheckerboard(b);
                b.SetGem(1, 3, new Gem(GemColor.Red));
                b.SetGem(2, 3, new Gem(GemColor.Red));
                b.SetGem(3, 3, new Gem(GemColor.Red));
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.True(res.AnyMatched, "Debe haber match");
                Assert.True(res.TotalGemsDestroyed >= 3, "Debe destruir al menos 3 gemas");
                Assert.True(res.MatchedColors.Contains(GemColor.Red), "Color rojo en MatchedColors");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: ColumnDestroyedCount cuenta destruidas por columna", () =>
            {
                Board b = new Board(10);
                FillCheckerboard(b);
                for (int i = 0; i < 3; i++) b.SetGem(3 + i, 3, new Gem(GemColor.Red));
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.True(res.AnyMatched, "Debe haber match");
                Assert.True(res.MatchedColumns.Contains(3) && res.MatchedColumns.Contains(4) && res.MatchedColumns.Contains(5), "Las 3 columnas deben estar en MatchedColumns");
                Assert.Equal(1, res.ColumnDestroyedCount[3], "Una gema destruida en col 3");
                Assert.Equal(1, res.ColumnDestroyedCount[4], "Una gema destruida en col 4");
                Assert.Equal(1, res.ColumnDestroyedCount[5], "Una gema destruida en col 5");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: la destruccion en cascada suma ColumnDestroyedCount", () =>
            {
                Board b = new Board(11);
                FillCheckerboard(b);
                // Two vertical matches in column 3: two gems each row pair (same column counts add up)
                for (int i = 0; i < 3; i++) b.SetGem(3, 1 + i, new Gem(GemColor.Green));
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.True(res.MatchedColumns.Contains(3), "Columna 3 debio matchearse");
                Assert.True(res.ColumnDestroyedCount[3] >= 3, "Debieron destruirse al menos 3 gemas en la col 3");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: match-4 crea Gema de Fuego", () =>
            {
                Board b = new Board(9);
                FillCheckerboard(b);
                for (int i = 0; i < 4; i++) b.SetGem(1 + i, 3, new Gem(GemColor.Red));
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.True(res.FlameCreated >= 1, "Match-4 debe crear llama");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: match-5 crea Supernova", () =>
            {
                Board b = new Board(10);
                FillCheckerboard(b);
                for (int i = 0; i < 5; i++) b.SetGem(1 + i, 3, new Gem(GemColor.Red));
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.True(res.SupernovaCreated >= 1, "Match-5 debe crear supernova");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: match-6 crea Hipercubo", () =>
            {
                Board b = new Board(11);
                FillCheckerboard(b);
                for (int i = 0; i < 6; i++) b.SetGem(1 + i, 3, new Gem(GemColor.Red));
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.True(res.HypercubeCreated >= 1, "Match-6 debe crear hipercubo");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: forma T crea Supernova", () =>
            {
                Board b = new Board(12);
                FillCheckerboard(b);
                b.SetGem(2, 3, new Gem(GemColor.Green));
                b.SetGem(3, 3, new Gem(GemColor.Green));
                b.SetGem(4, 3, new Gem(GemColor.Green));
                b.SetGem(3, 2, new Gem(GemColor.Green));
                b.SetGem(3, 4, new Gem(GemColor.Green));
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.True(res.SupernovaCreated >= 1, "Forma T debe crear supernova");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: matches simultaneos cuentan gemas y colores", () =>
            {
                Board b = new Board(13);
                FillCheckerboard(b);
                for (int i = 0; i < 3; i++) b.SetGem(1 + i, 3, new Gem(GemColor.Red));
                for (int i = 0; i < 3; i++) b.SetGem(1 + i, 4, new Gem(GemColor.Blue));
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.True(res.CascadeDepth >= 1, "Profundidad >= 1");
                Assert.True(res.TotalGemsDestroyed >= 6, "Al menos 6 gemas destruidas");
                Assert.True(res.MatchedColors.Contains(GemColor.Red) && res.MatchedColors.Contains(GemColor.Blue), "Ambos colores en MatchedColors");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: gravedad rellena el tablero sin huecos", () =>
            {
                Board b = new Board(14);
                FillCheckerboard(b);
                b.SetGem(1, 3, new Gem(GemColor.Red));
                b.SetGem(2, 3, new Gem(GemColor.Red));
                b.SetGem(3, 3, new Gem(GemColor.Red));
                b.ProcessMatchesAndGravity();
                for (int y = 0; y < Board.Rows; y++)
                    for (int x = 0; x < Board.Cols; x++)
                        Assert.NotNull(b.GetGem(x, y), "Hueco en " + x + "," + y);
            }));

            tests.Add(Tuple.Create<string, Action>("Board: explosiones en cascada son encadenadas y detienen", () =>
            {
                // A cascade that destroys a Flame must trigger its 3x3 blast on the
                // same resolution, so more than the 3 matched gems get destroyed.
                Board b = new Board(31);
                FillCheckerboard(b);
                b.SetGem(2, 2, new Gem(GemColor.Red)); // will be inside the blast
                b.SetGem(3, 2, new Gem(GemColor.Red)); // will be inside the blast
                b.SetGem(4, 2, new Gem(GemColor.Red)); // match (row 2, cols 2-4)
                b.SetGem(2, 3, new Gem(GemColor.Red));
                b.SetGem(3, 3, new Gem(GemColor.Red, SpecialType.Flame));
                b.SetGem(4, 3, new Gem(GemColor.Red));
                b.SetGem(2, 4, new Gem(GemColor.Red)); // will be inside the blast
                b.SetGem(3, 4, new Gem(GemColor.Red)); // will be inside the blast
                b.SetGem(4, 4, new Gem(GemColor.Red)); // will be inside the blast
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.True(res.TotalGemsDestroyed >= 9, "La explosion del Flame debe destruir el 3x3 (>=9), got " + res.TotalGemsDestroyed);
                Assert.True(res.CascadeDepth >= 1, "Debo de registrarse al menos una pasada de cascada");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: bomba sin explosiones devuelve 0", () =>
            {
                Board b = new Board(33);
                FillCheckerboard(b);
                Assert.Equal(0, b.TickBombs(), "Sin bombas no debe haber explosiones");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: ciclo completo de BOMBA (decrece y explota al 0)", () =>
            {
                Board b = new Board(34);
                FillCheckerboard(b);
                b.SetGem(4, 4, new Gem(GemColor.Red, SpecialType.Bomb, 3));
                var before = b.GetBombInfo();
                Assert.Equal(1, before.Count, "Debe existir 1 bomba");
                Assert.Equal(3, before[0].Item3, "Temporizador inicial 3");

                int ticks = 0;
                while (ticks < 3 && b.TickBombs() == 0) { ticks++; }
                Assert.Equal(2, ticks, "Debieron pasar 2 ticks sin explosion");
                var after = b.GetBombInfo();
                Assert.Equal(0, after.Count, "La bomba exploto y ya no debe existir");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: E2E motor (hint -> swap -> procesar -> tablero lleno)", () =>
            {
                for (int seed = 1; seed <= 5; seed++)
                {
                    Board b = new Board(seed);
                    var hint = HintFinder.FindValidMove(b);
                    Assert.NotNull(hint, "Semilla " + seed + " debe tener un movimiento valido");
                    Assert.True(b.SwapGems(hint.Value.FromX, hint.Value.FromY, hint.Value.ToX, hint.Value.ToY), "Semilla " + seed + ": swap simulado debe tener exito");
                    CascadeResult res = b.ProcessMatchesAndGravity();
                    for (int y = 0; y < Board.Rows; y++)
                        for (int x = 0; x < Board.Cols; x++)
                            Assert.NotNull(b.GetGem(x, y), "Hueco tras cascada en " + x + "," + y + " (seed " + seed + ")");
                    Assert.True(res.AnyMatched, "Semilla " + seed + ": la cascada debe detectar un match");
                    Assert.True(res.TotalGemsDestroyed >= 3, "Semilla " + seed + ": debe destruir al menos 3 gemas");
                }
            }));

            tests.Add(Tuple.Create<string, Action>("Board: gema de tiempo suministra segundos extra", () =>
            {
                Board b = new Board(15);
                FillCheckerboard(b);
                b.SetGem(2, 3, new Gem(GemColor.Red));
                b.SetGem(3, 3, new Gem(GemColor.Red));
                b.SetGem(4, 3, new Gem(GemColor.Red, SpecialType.Time5));
                CascadeResult res = b.ProcessMatchesAndGravity(true);
                Assert.True(res.TimeGemsMatched >= 1, "Gema Time5 debe contar");
                Assert.True(res.ExtraTimeSeconds >= 5, "Debe sumar 5 segundos");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: tierra adyacente se limpia", () =>
            {
                Board b = new Board(16);
                FillCheckerboard(b);
                b.SetGem(2, 4, new Gem(GemColor.Green));
                b.SetGem(3, 4, new Gem(GemColor.Green));
                b.SetGem(4, 4, new Gem(GemColor.Green));
                b.SetGem(3, 5, new Gem(GemColor.Yellow, SpecialType.Dirt));
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.True(res.DirtCleared >= 1, "La tierra adyacente debe limpiarse");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: roca dura se rompe con impacto adyacente", () =>
            {
                Board b = new Board(17);
                FillCheckerboard(b);
                b.SetGem(3, 6, new Gem(GemColor.Yellow, SpecialType.HardRock));
                for (int i = 0; i < 3; i++) b.SetGem(2 + i, 5, new Gem(GemColor.Green));
                for (int i = 0; i < 3; i++) b.SetGem(2 + i, 7, new Gem(GemColor.Green));
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.True(res.RockCleared >= 1, "Impacto adyacente debe romper la roca");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: sin match no hay actividad", () =>
            {
                Board b = new Board(18);
                FillCheckerboard(b);
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.False(res.AnyMatched, "No debe haber match");
                Assert.Equal(0, res.TotalGemsDestroyed, "Nada destruido");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: TriggerHypercubeColor elimina el color", () =>
            {
                Board b = new Board(19);
                FillCheckerboard(b);
                b.SetGem(0, 0, new Gem(GemColor.Red));
                b.SetGem(7, 7, new Gem(GemColor.Red));
                b.TriggerHypercubeColor(GemColor.Red);
                for (int y = 0; y < Board.Rows; y++)
                    for (int x = 0; x < Board.Cols; x++)
                        Assert.True(b.GetGem(x, y) == null || b.GetGem(x, y).Color != GemColor.Red, "Queda roja en " + x + "," + y);
            }));

            tests.Add(Tuple.Create<string, Action>("Board: swap de hipercubo con gema dispara su color", () =>
            {
                Board b = new Board(20);
                FillCheckerboard(b);
                b.SetGem(3, 3, new Gem(GemColor.Red, SpecialType.Hypercube));
                b.SetGem(4, 3, new Gem(GemColor.Red));
                Assert.True(b.SwapGems(3, 3, 4, 3), "Swap con hipercubo siempre permitido");
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.True(res.AnyMatched, "El disparo debe contar como match");
                for (int y = 0; y < Board.Rows; y++)
                    for (int x = 0; x < Board.Cols; x++)
                        Assert.True(b.GetGem(x, y) == null || b.GetGem(x, y).Color != GemColor.Red, "Queda roja tras disparo");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: hipercubo + hipercubo aniquila el tablero", () =>
            {
                Board b = new Board(21);
                FillCheckerboard(b);
                b.SetGem(3, 3, new Gem(GemColor.Red, SpecialType.Hypercube));
                b.SetGem(4, 3, new Gem(GemColor.Blue, SpecialType.Hypercube));
                Assert.True(b.SwapGems(3, 3, 4, 3), "Swap hipercubo con hipercubo");
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.True(res.AnyMatched, "Aniquilacion cuenta como match");
                Assert.True(res.TotalGemsDestroyed >= 60, "Debe destruir casi todo el tablero");
                Assert.True(res.AnnihilatorUsed, "El aniquilador debe marcarse al unir hipercubos");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: destruir una gema de fuego cuenta FlameDestroyed", () =>
            {
                Board b = new Board(22);
                FillCheckerboard(b);
                b.SetGem(0, 5, new Gem(GemColor.Red));
                b.SetGem(1, 5, new Gem(GemColor.Red, SpecialType.Flame));
                b.SetGem(2, 5, new Gem(GemColor.Red));
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.True(res.AnyMatched, "Match horizontal de 3");
                Assert.Equal(1, res.FlameDestroyed, "La gema de fuego destruida debe contarse");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: hipercubo sin swap NO se auto-dispara", () =>
            {
                Board b = new Board(22);
                FillCheckerboard(b);
                b.SetGem(3, 3, new Gem(GemColor.Red, SpecialType.Hypercube));
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.False(res.AnyMatched, "No debe dispararse sin swap");
                Assert.Equal(0, res.TotalGemsDestroyed, "Nada destruido");
                Assert.Equal(SpecialType.Hypercube, b.GetGem(3, 3).Special, "El hipercubo debe permanecer");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: tablero Mina de Diamantes (3 filas de tierra/roca)", () =>
            {
                Board b = new Board(23);
                b.InitializeDiamondMineBoard();
                for (int x = 0; x < Board.Cols; x++)
                {
                    Assert.True(b.GetGem(x, 5).Special == SpecialType.Dirt || b.GetGem(x, 5).Special == SpecialType.GoldNugget, "Fila 5 tierra/pepita");
                    Assert.True(b.GetGem(x, 6).Special == SpecialType.Dirt || b.GetGem(x, 6).Special == SpecialType.HardRock || b.GetGem(x, 6).Special == SpecialType.GoldNugget, "Fila 6 tierra/roca/pepita");
                    Assert.True(b.GetGem(x, 7).Special == SpecialType.HardRock || b.GetGem(x, 7).Special == SpecialType.GoldNugget, "Fila 7 roca/pepita");
                }
                Assert.True(b.HasDirtRemaining(), "Debe haber tierra restante");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: ShiftDiamondMineDown revela tierra nueva", () =>
            {
                Board b = new Board(24);
                b.InitializeDiamondMineBoard();
                b.ShiftDiamondMineDown();
                Assert.True(b.HasDirtRemaining(), "Debe seguir habiendo tierra");
                for (int x = 0; x < Board.Cols; x++)
                {
                    Assert.True(b.GetGem(x, 6).Special == SpecialType.Dirt || b.GetGem(x, 6).Special == SpecialType.GoldNugget, "Fila 6 nueva tierra/pepita");
                    Assert.True(b.GetGem(x, 7).Special == SpecialType.Dirt || b.GetGem(x, 7).Special == SpecialType.HardRock || b.GetGem(x, 7).Special == SpecialType.GoldNugget, "Fila 7 tierra/roca/pepita");
                }
            }));

            tests.Add(Tuple.Create<string, Action>("Board: GoldRush repone pepitas al bajar la mina", () =>
            {
                Board b = new Board(240);
                b.InitializeDiamondMineBoard();
                int cumulative = 0;
                for (int i = 0; i < 40; i++)
                {
                    b.ShiftDiamondMineDown();
                    for (int x = 0; x < Board.Cols; x++)
                    {
                        if (b.GetGem(x, 6) != null && b.GetGem(x, 6).Special == SpecialType.GoldNugget) cumulative++;
                        if (b.GetGem(x, 7) != null && b.GetGem(x, 7).Special == SpecialType.GoldNugget) cumulative++;
                    }
                }
                Assert.True(cumulative > 0, "Deben surgir pepitas nuevas al excavar");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: la tierra de la mina no crea matches fantasma", () =>
            {
                Board b = new Board(23);
                b.InitializeDiamondMineBoard();
                Assert.False(b.HasAnyMatch(), "Filas uniformes de tierra/roca no deben contar como match");
                CascadeResult res = b.ProcessMatchesAndGravity();
                Assert.Equal(0, res.TotalGemsDestroyed, "No debe destruirse nada sin swap");
                Assert.False(res.AnyMatched, "Sin swap no hay cascada");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: InitializeBoard con bombas mantiene el campo de bombas (Time Bomb scramble)", () =>
            {
                Board b = new Board(230);
                b.InitializeBoard(true);
                Assert.True(b.GetBombCount() >= 2, "El rescramble de Time Bomb debe conservar bombas frescas");
                Assert.False(b.HasAnyMatch(), "El tablero regenerado no debe tener matches iniciales");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: mariposas suben una fila por turno", () =>
            {
                Board b = new Board(25);
                FillCheckerboard(b);
                b.SetGem(3, 4, new Gem(GemColor.Green, SpecialType.None, 0, true));
                b.MoveButterfliesUp();
                Assert.True(b.GetGem(3, 3) != null && b.GetGem(3, 3).IsButterfly, "Mariposa debe subir a (3,3)");
                Assert.False(b.GetGem(3, 4).IsButterfly, "La celda anterior queda libre de mariposa");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: deteccion de mariposa en la cima", () =>
            {
                Board b = new Board(26);
                FillCheckerboard(b);
                Assert.False(b.IsButterflyAtTop(), "No debe haber mariposa arriba");
                b.SetGem(5, 0, new Gem(GemColor.Green, SpecialType.None, 0, true));
                Assert.True(b.IsButterflyAtTop(), "Debe detectar mariposa en la fila superior");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: tablero de mariposas inicia con mariposas", () =>
            {
                Board b = new Board(27);
                b.InitializeButterfliesBoard();
                Assert.True(b.GetButterflyCount() >= 4, "Deben haber al menos 4 mariposas al inicio");
                Assert.False(b.HasAnyMatch(), "El tablero no debe tener matches iniciales");
                Assert.NotNull(HintFinder.FindValidMove(b), "Debe haber al menos un movimiento");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: las mariposas se reponen desde el fondo tras liberarlas", () =>
            {
                Board b = new Board(271);
                b.InitializeButterfliesBoard();
                int initial = b.GetButterflyCount();

                // Free them all by clearing every butterfly cell.
                for (int y = 0; y < Board.Rows; y++)
                {
                    for (int x = 0; x < Board.Cols; x++)
                    {
                        Gem g = b.GetGem(x, y);
                        if (g != null && g.IsButterfly) b.SetGem(x, y, null);
                    }
                }
                Assert.Equal(0, b.GetButterflyCount(), "Todas libradas");
                Assert.Equal(0, b.GetButterflyColumns().Count, "Sin columnas de mariposas");

                // Replenish like the turn flow does and verify a stream returns.
                int guard = 0;
                while (b.GetButterflyCount() < 6 && guard < 12)
                {
                    b.SpawnButterflyAtBottom();
                    guard++;
                }
                Assert.True(b.GetButterflyCount() >= 4, "El tablero vuelve a poblarse de mariposas");
                Assert.True(initial <= 6, "Poblacion inicial razonable");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: peligro de mariposa en fila 1", () =>
            {
                Board b = new Board(28);
                FillCheckerboard(b);
                Assert.False(b.IsButterflyInDanger(), "Sin mariposas en fila 1 no hay peligro");
                b.SetGem(2, 1, new Gem(GemColor.Green, SpecialType.None, 0, true));
                Assert.True(b.IsButterflyInDanger(), "Mariposa en fila 1 debe ser peligro");
                List<int> cols = b.GetButterflyDangerColumns();
                Assert.True(cols.Contains(2), "Columna 2 debe estar en peligro");
            }));

            tests.Add(Tuple.Create<string, Action>("Board: conteo y columnas de mariposas", () =>
            {
                Board b = new Board(29);
                FillCheckerboard(b);
                b.SetGem(3, 2, new Gem(GemColor.Green, SpecialType.None, 0, true));
                b.SetGem(6, 5, new Gem(GemColor.Blue, SpecialType.None, 0, true));
                b.SetGem(3, 7, new Gem(GemColor.White, SpecialType.None, 0, true));
                Assert.Equal(3, b.GetButterflyCount(), "Tres mariposas");
                List<int> cols = b.GetButterflyColumns();
                Assert.Equal(2, cols.Count, "Dos columnas distintas");
                Assert.True(cols.Contains(3) && cols.Contains(6), "Columnas 3 y 6");
            }));

            // ======================= HINT FINDER =======================
            tests.Add(Tuple.Create<string, Action>("HintFinder: encuentra movimiento en tablero preparado", () =>
            {
                Board b = new Board(100);
                FillCheckerboard(b);
                b.SetGem(2, 3, new Gem(GemColor.Red));
                b.SetGem(3, 3, new Gem(GemColor.Red));
                b.SetGem(3, 4, new Gem(GemColor.Red));
                b.SetGem(4, 4, new Gem(GemColor.Red));
                MoveHint? hint = HintFinder.FindValidMove(b);
                Assert.True(hint.HasValue, "Debe encontrar una pista");
            }));

            tests.Add(Tuple.Create<string, Action>("HintFinder: tablero sin movimientos devuelve null", () =>
            {
                Board b = new Board(101);
                GemColor[] ciclo = { GemColor.Red, GemColor.Yellow, GemColor.Green, GemColor.Blue, GemColor.Purple, GemColor.White, GemColor.Orange };
                for (int y = 0; y < Board.Rows; y++)
                    for (int x = 0; x < Board.Cols; x++)
                        b.SetGem(x, y, new Gem(ciclo[(x + y) % ciclo.Length]));
                Assert.False(b.HasAnyMatch(), "Patron rotado no debe tener matches");
                MoveHint? hint = HintFinder.FindValidMove(b);
                Assert.True(!hint.HasValue, "Tablero sin movimientos no debe devolver hint");
            }));

            tests.Add(Tuple.Create<string, Action>("HintFinder: GetValidMovesFrom detecta direcciones validas", () =>
            {
                Board b = new Board(102);
                GemColor[] ciclo = { GemColor.Red, GemColor.Yellow, GemColor.Green, GemColor.Blue, GemColor.Purple, GemColor.White, GemColor.Orange };
                for (int y = 0; y < Board.Rows; y++)
                    for (int x = 0; x < Board.Cols; x++)
                        b.SetGem(x, y, new Gem(ciclo[(x + y) % ciclo.Length]));

                var none = HintFinder.GetValidMovesFrom(b, 2, 3);
                Assert.Equal(0, none.Count, "Sin configuracion no debe haber movimientos");

                // (4,3) ya es Roja en el patron; al colocar rojas en (2,3) y (5,3),
                // mover (2,3) hacia la derecha completa la fila (3,3),(4,3),(5,3)
                b.SetGem(2, 3, new Gem(GemColor.Red));
                b.SetGem(5, 3, new Gem(GemColor.Red));

                var moves = HintFinder.GetValidMovesFrom(b, 2, 3);
                bool hasRight = false;
                foreach (var m in moves)
                    if (m.Key == 1 && m.Value == 0) hasRight = true;
                Assert.True(hasRight, "Swap con (3,3) hacia derecha debe ser valido");
            }));

            // ======================= POKER =======================
            tests.Add(Tuple.Create<string, Action>("Poker: Escalera de Color (Flush)", () =>
            {
                var cards = new List<GemColor> { GemColor.Red, GemColor.Red, GemColor.Red, GemColor.Red, GemColor.Red };
                Assert.Equal(PokerHandType.Flush, PokerHandEvaluator.Evaluate(cards), "Flush");
            }));

            tests.Add(Tuple.Create<string, Action>("Poker: Espectro (5 colores distintos)", () =>
            {
                var cards = new List<GemColor> { GemColor.Red, GemColor.Blue, GemColor.Green, GemColor.White, GemColor.Orange };
                Assert.Equal(PokerHandType.Spectrum, PokerHandEvaluator.Evaluate(cards), "Spectrum");
            }));

            tests.Add(Tuple.Create<string, Action>("Poker: Poker (FourOfAKind)", () =>
            {
                var cards = new List<GemColor> { GemColor.Red, GemColor.Red, GemColor.Red, GemColor.Red, GemColor.Blue };
                Assert.Equal(PokerHandType.FourOfAKind, PokerHandEvaluator.Evaluate(cards), "FourOfAKind");
            }));

            tests.Add(Tuple.Create<string, Action>("Poker: Full House", () =>
            {
                var cards = new List<GemColor> { GemColor.Red, GemColor.Red, GemColor.Red, GemColor.Blue, GemColor.Blue };
                Assert.Equal(PokerHandType.FullHouse, PokerHandEvaluator.Evaluate(cards), "FullHouse");
            }));

            tests.Add(Tuple.Create<string, Action>("Poker: Tercia (ThreeOfAKind)", () =>
            {
                var cards = new List<GemColor> { GemColor.Red, GemColor.Red, GemColor.Red, GemColor.Blue, GemColor.Green };
                Assert.Equal(PokerHandType.ThreeOfAKind, PokerHandEvaluator.Evaluate(cards), "ThreeOfAKind");
            }));

            tests.Add(Tuple.Create<string, Action>("Poker: Doble pareja", () =>
            {
                var cards = new List<GemColor> { GemColor.Red, GemColor.Red, GemColor.Blue, GemColor.Blue, GemColor.Green };
                Assert.Equal(PokerHandType.TwoPair, PokerHandEvaluator.Evaluate(cards), "TwoPair");
            }));

            tests.Add(Tuple.Create<string, Action>("Poker: Pareja", () =>
            {
                var cards = new List<GemColor> { GemColor.Red, GemColor.Red, GemColor.Blue, GemColor.Green, GemColor.White };
                Assert.Equal(PokerHandType.Pair, PokerHandEvaluator.Evaluate(cards), "Pair");
            }));

            tests.Add(Tuple.Create<string, Action>("Poker: lista corta o null -> HighCard", () =>
            {
                Assert.Equal(PokerHandType.HighCard, PokerHandEvaluator.Evaluate(new List<GemColor> { GemColor.Red }), "Corta");
                Assert.Equal(PokerHandType.HighCard, PokerHandEvaluator.Evaluate(null), "Null");
            }));

            tests.Add(Tuple.Create<string, Action>("Poker: puntos por mano exactos", () =>
            {
                Assert.Equal(2500, PokerHandEvaluator.GetHandPoints(PokerHandType.Flush), "Flush");
                Assert.Equal(2000, PokerHandEvaluator.GetHandPoints(PokerHandType.FourOfAKind), "FourOfAKind");
                Assert.Equal(1500, PokerHandEvaluator.GetHandPoints(PokerHandType.FullHouse), "FullHouse");
                Assert.Equal(1000, PokerHandEvaluator.GetHandPoints(PokerHandType.ThreeOfAKind), "ThreeOfAKind");
                Assert.Equal(750, PokerHandEvaluator.GetHandPoints(PokerHandType.TwoPair), "TwoPair");
                Assert.Equal(500, PokerHandEvaluator.GetHandPoints(PokerHandType.Spectrum), "Spectrum");
                Assert.Equal(250, PokerHandEvaluator.GetHandPoints(PokerHandType.Pair), "Pair");
                Assert.Equal(100, PokerHandEvaluator.GetHandPoints(PokerHandType.HighCard), "HighCard");
            }));

            // ======================= RANK SYSTEM =======================
            tests.Add(Tuple.Create<string, Action>("RankSystem: calculo de nivel", () =>
            {
                Assert.Equal(1, RankSystem.GetRankLevel(0), "0 puntos");
                Assert.Equal(1, RankSystem.GetRankLevel(-5), "Negativo");
                Assert.Equal(1, RankSystem.GetRankLevel(249999), "249999");
                Assert.Equal(2, RankSystem.GetRankLevel(250000), "250000");
                Assert.Equal(2, RankSystem.GetRankLevel(749999), "749999");
                Assert.Equal(3, RankSystem.GetRankLevel(750000), "750000");
                Assert.Equal(3, RankSystem.GetRankLevel(1499999), "1499999");
                Assert.Equal(4, RankSystem.GetRankLevel(1500000), "1500000");
                Assert.Equal(5, RankSystem.GetRankLevel(2500000), "2500000");
            }));

            tests.Add(Tuple.Create<string, Action>("RankSystem: nivel maximo limitado al numero de titulos", () =>
            {
                int maxLevel = RankSystem.GetRankLevel(int.MaxValue);
                Assert.True(maxLevel == 131, "Capa en 131 titulos, got " + maxLevel);
                Assert.Equal(131, RankSystem.GetRankLevel(2140000000), "Saturacion a puntuacion maxima real");
            }));

            tests.Add(Tuple.Create<string, Action>("RankSystem: titulos no vacios y progresivos", () =>
            {
                string r1 = RankSystem.GetRankTitle(0);
                string r2 = RankSystem.GetRankTitle(250000);
                Assert.True(!string.IsNullOrEmpty(r1) && !string.IsNullOrEmpty(r2), "Titulos no vacios");
                Assert.True(r1 != r2, "Titulos deben diferir entre niveles");
            }));

            tests.Add(Tuple.Create<string, Action>("RankSystem: todos los titulos en espanol", () =>
            {
                string final = RankSystem.GetRankTitle(int.MaxValue);
                Assert.True(final.IndexOf("Elder", StringComparison.OrdinalIgnoreCase) < 0, "El titulo final debe estar en espanol");
                Assert.Equal("Nivel 131: Anciano Bejeweliano", final, "Titulo final traducido");
                Assert.Equal("Nivel 1: Novato", RankSystem.GetRankTitle(0), "Titulo inicial");
            }));

            tests.Add(Tuple.Create<string, Action>("Localization: nombres de manos de poker en espanol", () =>
            {
                Localization.CurrentLanguage = Language.Spanish;
                Assert.Equal("Carta Alta", Localization.GetPokerHandName(PokerHandType.HighCard), "HighCard");
                Assert.Equal("Pareja", Localization.GetPokerHandName(PokerHandType.Pair), "Pair");
                Assert.Equal("Espectro", Localization.GetPokerHandName(PokerHandType.Spectrum), "Spectrum");
                Assert.Equal("Doble Pareja", Localization.GetPokerHandName(PokerHandType.TwoPair), "TwoPair");
                Assert.Equal("Trío", Localization.GetPokerHandName(PokerHandType.ThreeOfAKind), "ThreeOfAKind");
                Assert.Equal("Full House", Localization.GetPokerHandName(PokerHandType.FullHouse), "FullHouse");
                Assert.Equal("Póker", Localization.GetPokerHandName(PokerHandType.FourOfAKind), "FourOfAKind");
                Assert.Equal("Color", Localization.GetPokerHandName(PokerHandType.Flush), "Flush");
            }));

            // ======================= LOCALIZATION =======================
            tests.Add(Tuple.Create<string, Action>("Localization: idioma por defecto espanol", () =>
            {
                Localization.CurrentLanguage = Language.Spanish;
                Assert.Equal(Language.Spanish, Localization.CurrentLanguage, "Idioma inicial");
            }));

            tests.Add(Tuple.Create<string, Action>("Localization: espanol e ingles difieren", () =>
            {
                Localization.CurrentLanguage = Language.Spanish;
                string es = Localization.Get("MenuPlay");
                Localization.CurrentLanguage = Language.English;
                string en = Localization.Get("MenuPlay");
                Assert.True(es != en && !string.IsNullOrEmpty(es) && !string.IsNullOrEmpty(en), "Traducciones deben diferir");
            }));

            tests.Add(Tuple.Create<string, Action>("Localization: formato con argumentos", () =>
            {
                Localization.CurrentLanguage = Language.Spanish;
                string s = Localization.Get("QuestStatusNuggets", 3, 5);
                Assert.True(s.Contains("3"), "Debe interpolar argumentos: " + s);
            }));

            tests.Add(Tuple.Create<string, Action>("Localization: barra eliminadora de poker anuncia calavera", () =>
            {
                Localization.CurrentLanguage = Language.Spanish;
                string s = Localization.Get("PokerSkullEliminated", 4);
                Assert.True(s.Contains("4"), "Debe interpolar calaveras restantes: " + s);
                string status = Localization.Get("PokerStatus", 100, 3, 1, 2);
                Assert.True(status.Contains("2") && status.Contains("eliminadora"), "Debe interpolar carga: " + status);
                Localization.CurrentLanguage = Language.English;
                Assert.True(Localization.Get("PokerSkullEliminated", 4).Contains("Skull"), "Traduccion ingles existente");
                Localization.CurrentLanguage = Language.Spanish;
            }));

            tests.Add(Tuple.Create<string, Action>("Localization: clave desconocida devuelve la clave", () =>
            {
                Assert.Equal("ClaveInexistenteXYZ", Localization.Get("ClaveInexistenteXYZ"), "Key fallback");
            }));

            tests.Add(Tuple.Create<string, Action>("Localization: anuncio de rango nuevo en ambos idiomas", () =>
            {
                Localization.CurrentLanguage = Language.Spanish;
                string es = Localization.Get("RankUpAnnouncement", RankSystem.GetRankTitle(250000));
                Assert.True(es.Contains("Nuevo rango alcanzado") && es.Contains("Aprendiz"), "Anuncio ES: " + es);
                Localization.CurrentLanguage = Language.English;
                string en = Localization.Get("RankUpAnnouncement", RankSystem.GetRankTitle(250000));
                Assert.True(en.Contains("New rank reached") && en.Contains("Apprentice"), "Anuncio EN: " + en);
                Localization.CurrentLanguage = Language.Spanish;
            }));

            tests.Add(Tuple.Create<string, Action>("Localization: ToggleLanguage alterna", () =>
            {
                Localization.CurrentLanguage = Language.Spanish;
                Localization.ToggleLanguage();
                Assert.Equal(Language.English, Localization.CurrentLanguage, "Alternar a ingles");
                Localization.ToggleLanguage();
                Assert.Equal(Language.Spanish, Localization.CurrentLanguage, "Alternar a espanol");
            }));

            // ======================= GEM =======================
            tests.Add(Tuple.Create<string, Action>("Gem: temporizador de bomba por defecto 15", () =>
            {
                Gem g = new Gem(GemColor.Red, SpecialType.Bomb);
                Assert.Equal(15, g.BombTimer, "Timer bomba");
            }));

            tests.Add(Tuple.Create<string, Action>("Gem: temporizador de bomba personalizado se conserva", () =>
            {
                Gem g = new Gem(GemColor.Red, SpecialType.Bomb, 7);
                Assert.Equal(7, g.BombTimer, "Timer custom");
            }));

            tests.Add(Tuple.Create<string, Action>("Gem: durabilidad roca dura 1 y tierra 1", () =>
            {
                Assert.Equal(1, new Gem(GemColor.Red, SpecialType.HardRock).RockDurability, "Roca");
                Assert.Equal(1, new Gem(GemColor.Red, SpecialType.GoldNugget).RockDurability, "Pepita");
                Assert.Equal(1, new Gem(GemColor.Red, SpecialType.Dirt).RockDurability, "Tierra");
            }));

            tests.Add(Tuple.Create<string, Action>("Gem: Clone es independiente", () =>
            {
                Gem g = new Gem(GemColor.Red, SpecialType.Flame, 5, true);
                Gem c = g.Clone();
                c.Color = GemColor.Blue;
                c.Special = SpecialType.None;
                Assert.Equal(GemColor.Red, g.Color, "Original no afectado (color)");
                Assert.Equal(SpecialType.Flame, g.Special, "Original no afectado (especial)");
            }));

            tests.Add(Tuple.Create<string, Action>("Gem: nombres localizados para tipos especiales", () =>
            {
                Localization.CurrentLanguage = Language.Spanish;
                Assert.True(!string.IsNullOrEmpty(new Gem(GemColor.Red).GetNameLocalized()), "Normal");
                Assert.True(!string.IsNullOrEmpty(new Gem(GemColor.Red, SpecialType.Flame).GetNameLocalized()), "Flame");
                Assert.True(!string.IsNullOrEmpty(new Gem(GemColor.Red, SpecialType.Hypercube).GetNameLocalized()), "Hypercube");
                Assert.True(!string.IsNullOrEmpty(new Gem(GemColor.Red, SpecialType.Star).GetNameLocalized()), "Star");
                Assert.True(!string.IsNullOrEmpty(new Gem(GemColor.Red, SpecialType.Supernova).GetNameLocalized()), "Supernova");
                Assert.True(!string.IsNullOrEmpty(new Gem(GemColor.Red, SpecialType.Time5).GetNameLocalized()), "Time5");
                Assert.True(!string.IsNullOrEmpty(new Gem(GemColor.Red, SpecialType.Time10).GetNameLocalized()), "Time10");
                Assert.True(!string.IsNullOrEmpty(new Gem(GemColor.Red, SpecialType.Dirt).GetNameLocalized()), "Dirt");
                Assert.True(!string.IsNullOrEmpty(new Gem(GemColor.Red, SpecialType.HardRock).GetNameLocalized()), "HardRock");
                Assert.True(!string.IsNullOrEmpty(new Gem(GemColor.Red, SpecialType.None, 0, true).GetNameLocalized()), "Butterfly");
            }));

            // ======================= BADGE MANAGER =======================
            tests.Add(Tuple.Create<string, Action>("Badge: estado inicial bloqueado", () =>
            {
                BadgeManager bm = new BadgeManager();
                Assert.Equal(BadgeTier.Locked, bm.GetTier("BadgeInferno"), "Tier inicial");
            }));

            tests.Add(Tuple.Create<string, Action>("Badge: subida de tier y rechazo de bajada", () =>
            {
                BadgeManager bm = new BadgeManager();
                Assert.True(bm.SetTierIfHigher("BadgeInferno", BadgeTier.Bronze), "Asignar Bronce");
                Assert.False(bm.SetTierIfHigher("BadgeInferno", BadgeTier.Locked), "No bajar a Locked");
                Assert.Equal(BadgeTier.Bronze, bm.GetTier("BadgeInferno"), "Sigue Bronce");
                Assert.True(bm.SetTierIfHigher("BadgeInferno", BadgeTier.Gold), "Subir a Oro");
                Assert.Equal(BadgeTier.Gold, bm.GetTier("BadgeInferno"), "Ahora Oro");
            }));

            tests.Add(Tuple.Create<string, Action>("Badge: persistencia roundtrip sin tocar AppData", () =>
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "Bj3Tests_" + Guid.NewGuid().ToString("N"));
                try
                {
                    BadgeManager.OverrideDataDirectory = tempDir;
                    BadgeManager bm = new BadgeManager();
                    bm.SetTierIfHigher("BadgeStellar", BadgeTier.Platinum);
                    bm.Save("TestPlayer");
                    BadgeManager loaded = BadgeManager.Load("TestPlayer");
                    Assert.Equal(BadgeTier.Platinum, loaded.GetTier("BadgeStellar"), "Tier persistido");
                }
                finally
                {
                    BadgeManager.OverrideDataDirectory = null;
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                }
            }));

            tests.Add(Tuple.Create<string, Action>("Badge: archivo inexistente devuelve estado vacio", () =>
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "Bj3Tests_" + Guid.NewGuid().ToString("N"));
                try
                {
                    BadgeManager.OverrideDataDirectory = tempDir;
                    BadgeManager loaded = BadgeManager.Load("SinDatos");
                    Assert.NotNull(loaded, "No debe ser null");
                    Assert.Equal(BadgeTier.Locked, loaded.GetTier("BadgeInferno"), "Sin badges");
                }
                finally
                {
                    BadgeManager.OverrideDataDirectory = null;
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                }
            }));

            // ======================= GAME PROGRESS =======================
            tests.Add(Tuple.Create<string, Action>("Progress: valores por defecto", () =>
            {
                GameProgress gp = new GameProgress();
                Assert.Equal(1, gp.ClassicLevel, "ClassicLevel");
                Assert.Equal(1, gp.ZenLevel, "ZenLevel");
                Assert.Equal(0, gp.LightningHighScore, "LightningHighScore");
                Assert.Equal(0, gp.QuestRelicCount, "QuestRelicCount");
            }));

            tests.Add(Tuple.Create<string, Action>("Progress: desbloqueos por umbral", () =>
            {
                GameProgress gp = new GameProgress();
                Assert.False(gp.IsPokerUnlocked && gp.IsButterfliesUnlocked && gp.IsIceStormUnlocked && gp.IsDiamondMineUnlocked, "Todo bloqueado");
                gp.ClassicLevel = 5;
                gp.ZenLevel = 5;
                gp.LightningHighScore = 100000;
                gp.QuestRelicCount = 4;
                Assert.True(gp.IsPokerUnlocked, "Poker nivel 5");
                Assert.True(gp.IsButterfliesUnlocked, "Mariposas Zen 5");
                Assert.True(gp.IsIceStormUnlocked, "Tormenta 100k");
                Assert.True(gp.IsDiamondMineUnlocked, "Mina 4 desafios");
                Assert.False(new GameProgress { ClassicLevel = 4 }.IsPokerUnlocked, "Nivel 4 no desbloquea poker");
            }));

            tests.Add(Tuple.Create<string, Action>("Progress: persistencia roundtrip sin tocar AppData", () =>
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "Bj3Tests_" + Guid.NewGuid().ToString("N"));
                try
                {
                    GameProgress.OverrideDataDirectory = tempDir;
                    GameProgress gp = new GameProgress();
                gp.ClassicLevel = 7;
                gp.LightningHighScore = 150000;
                gp.TotalScore = 42000;
                gp.QuestRelicCount = 4;
                gp.Save();
                GameProgress loaded = GameProgress.Load();
                Assert.Equal(7, loaded.ClassicLevel, "ClassicLevel persistido");
                Assert.Equal(150000, loaded.LightningHighScore, "Record persistido");
                Assert.Equal(42000, loaded.TotalScore, "Puntaje persistido");
                Assert.Equal(4, loaded.QuestRelicCount, "Relicarios persistidos");

                // Retro-compatibilidad: un archivo generado por builds anteriores
                // (elemento <QuestRelic1Completed>) debe seguir poblando el campo.
                File.WriteAllText(Path.Combine(tempDir, "Bejeweled3Accessible", "progress.xml"),
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?><GameProgress>" +
                    "<ClassicLevel>1</ClassicLevel><ZenLevel>1</ZenLevel><LightningHighScore>0</LightningHighScore>" +
                    "<PokerHighScore>0</PokerHighScore><ButterfliesHighScore>0</ButterfliesHighScore>" +
                    "<IceStormHighScore>0</IceStormHighScore><DiamondMineHighScore>0</DiamondMineHighScore>" +
                    "<QuestRelic1Completed>4</QuestRelic1Completed><TotalScore>0</TotalScore></GameProgress>");
                GameProgress legacy = GameProgress.Load();
                Assert.True(legacy.QuestRelicCount == 4, "XML name antiguo -> QuestRelicCount");
                Assert.True(legacy.IsDiamondMineUnlocked, "Desbloqueo desde legado");
                }
                finally
                {
                    GameProgress.OverrideDataDirectory = null;
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                }
            }));

            tests.Add(Tuple.Create<string, Action>("Progress: archivo corrupto devuelve valores por defecto", () =>
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "Bj3Tests_" + Guid.NewGuid().ToString("N"));
                try
                {
                    GameProgress.OverrideDataDirectory = tempDir;
                    string file = Path.Combine(tempDir, "Bejeweled3Accessible", "progress.xml");
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    File.WriteAllText(file, "esto no es xml valido <<<");
                    GameProgress loaded = GameProgress.Load();
                    Assert.Equal(1, loaded.ClassicLevel, "Fallback a defaults");
                }
                finally
                {
                    GameProgress.OverrideDataDirectory = null;
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                }
            }));

            // ======================= GAME OPTIONS =======================
            tests.Add(Tuple.Create<string, Action>("Options: valores por defecto", () =>
            {
                GameOptions opt = new GameOptions();
                Assert.Equal(80, opt.MusicVolume, "Musica");
                Assert.Equal(100, opt.SoundVolume, "Sonido");
                Assert.Equal(100, opt.VoiceVolume, "Voz");
                Assert.Equal(Language.Spanish, opt.SelectedLanguage, "Idioma");
            }));

            tests.Add(Tuple.Create<string, Action>("Options: persistencia roundtrip sin tocar AppData", () =>
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "Bj3Tests_" + Guid.NewGuid().ToString("N"));
                try
                {
                    GameOptions.OverrideDataDirectory = tempDir;
                    GameOptions opt = new GameOptions();
                    opt.MusicVolume = 35;
                    opt.SoundVolume = 55;
                    opt.SelectedLanguage = Language.English;
                    opt.Save();
                    GameOptions loaded = GameOptions.Load();
                    Assert.Equal(35, loaded.MusicVolume, "Musica persistida");
                    Assert.Equal(55, loaded.SoundVolume, "Sonido persistido");
                    Assert.Equal(Language.English, loaded.SelectedLanguage, "Idioma persistido");
                }
                finally
                {
                    GameOptions.OverrideDataDirectory = null;
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                }
            }));

            // ======================= PROFILE MANAGER =======================
            tests.Add(Tuple.Create<string, Action>("Profile: sin perfiles CurrentProfile es null", () =>
            {
                ProfileManager pm = new ProfileManager();
                Assert.True(pm.CurrentProfile == null, "Debe ser null");
            }));

            tests.Add(Tuple.Create<string, Action>("Profile: agregar y seleccionar perfil", () =>
            {
                ProfileManager pm = new ProfileManager();
                pm.Profiles.Add(new PlayerProfile("Ana"));
                pm.Profiles.Add(new PlayerProfile("Luis"));
                Assert.Equal("Ana", pm.CurrentProfile.ProfileName, "Primer perfil");
                pm.CurrentProfileIndex = 1;
                Assert.Equal("Luis", pm.CurrentProfile.ProfileName, "Segundo perfil");
            }));

            tests.Add(Tuple.Create<string, Action>("Profile: indice fuera de rango se reinicia", () =>
            {
                ProfileManager pm = new ProfileManager();
                pm.Profiles.Add(new PlayerProfile("Ana"));
                pm.CurrentProfileIndex = 99;
                Assert.Equal("Ana", pm.CurrentProfile.ProfileName, "Indice reiniciado");
            }));

            tests.Add(Tuple.Create<string, Action>("Profile: persistencia roundtrip sin tocar AppData", () =>
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "Bj3Tests_" + Guid.NewGuid().ToString("N"));
                try
                {
                    ProfileManager.OverrideDataDirectory = tempDir;
                    ProfileManager pm = new ProfileManager();
                    pm.Profiles.Add(new PlayerProfile("TestPlayer"));
                    pm.Profiles.Add(new PlayerProfile("Otro"));
                    pm.Save();
                    ProfileManager loaded = ProfileManager.Load();
                    Assert.Equal(2, loaded.Profiles.Count, "Cantidad de perfiles");
                    Assert.Equal("TestPlayer", loaded.Profiles[0].ProfileName, "Nombre persistido");
                }
                finally
                {
                    ProfileManager.OverrideDataDirectory = null;
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                }
            }));

            // ======================= PAC AUDIO ARCHIVE =======================
            tests.Add(Tuple.Create<string, Action>("PAC: empaquetar y leer roundtrip cifrado", () =>
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "Bj3Tests_" + Guid.NewGuid().ToString("N"));
                try
                {
                    byte[] soundBytes = { 0x4F, 0x67, 0x67, 0x53, 0x01, 0x02, 0x03, 0xFF, 0x00, 0xAA };
                    byte[] musicBytes = { 0x49, 0x44, 0x33, 0x11, 0x22, 0x33, 0x44, 0x55 };
                    string sDir = Path.Combine(tempDir, "sounds", "sounds");
                    string mDir = Path.Combine(tempDir, "music");
                    Directory.CreateDirectory(sDir);
                    Directory.CreateDirectory(mDir);
                    File.WriteAllBytes(Path.Combine(sDir, "select.ogg"), soundBytes);
                    File.WriteAllBytes(Path.Combine(mDir, "01 - Intro.mp3"), musicBytes);

                    string pacPath = Path.Combine(tempDir, "audio.pac");
                    PacPacker.PackDirectoriesToSinglePac(tempDir, pacPath, "sounds", "music");
                    Assert.True(File.Exists(pacPath), "PAC creado");

                    byte[] header = File.ReadAllBytes(pacPath);
                    Assert.True(header[0] == (byte)'P' && header[1] == (byte)'A' && header[2] == (byte)'C' && header[3] == (byte)'1', "Magic PAC1");

                    PacReader pac = new PacReader(pacPath);
                    Assert.True(BytesEqual(soundBytes, pac.GetFileBytes("sounds\\sounds\\select.ogg")), "Ruta completa");
                    Assert.True(BytesEqual(soundBytes, pac.GetFileBytes("select.ogg")), "Nombre archivo");
                    Assert.True(BytesEqual(soundBytes, pac.GetFileBytes("select")), "Sin extension");
                    Assert.True(BytesEqual(soundBytes, pac.GetFileBytes("SELECT")), "Mayusculas");
                    Assert.True(BytesEqual(musicBytes, pac.GetFileBytes("01 - Intro.mp3")), "Musica");
                }
                finally
                {
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                }
            }));

            tests.Add(Tuple.Create<string, Action>("PAC: archivo inexistente devuelve vacio", () =>
            {
                PacReader pac = new PacReader(Path.Combine(Path.GetTempPath(), "no_existe_123.pac"));
                Assert.True(pac.GetFileBytes("select.ogg") == null, "Debe devolver null");
            }));

            tests.Add(Tuple.Create<string, Action>("PAC: archivo basura se ignora sin excepcion", () =>
            {
                string tempFile = Path.Combine(Path.GetTempPath(), "Bj3Tests_" + Guid.NewGuid().ToString("N") + ".pac");
                File.WriteAllText(tempFile, "esto no es un pac valido");
                try
                {
                    PacReader pac = new PacReader(tempFile);
                    Assert.True(pac.GetFileBytes("select.ogg") == null, "Sin archivos");
                }
                finally
                {
                    if (File.Exists(tempFile)) File.Delete(tempFile);
                }
            }));

            tests.Add(Tuple.Create<string, Action>("PAC: ExtractToTempFile devuelve bytes correctos", () =>
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "Bj3Tests_" + Guid.NewGuid().ToString("N"));
                string extracted = null;
                try
                {
                    byte[] bytes = { 0x0F, 0x1E, 0x2D, 0x3C, 0x4B, 0x5A };
                    string sDir = Path.Combine(tempDir, "sounds", "sounds");
                    Directory.CreateDirectory(sDir);
                    File.WriteAllBytes(Path.Combine(sDir, "test.ogg"), bytes);
                    string pacPath = Path.Combine(tempDir, "audio.pac");
                    PacPacker.PackDirectoriesToSinglePac(tempDir, pacPath, "sounds");
                    PacReader pac = new PacReader(pacPath);
                    extracted = pac.ExtractToTempFile("test.ogg");
                    Assert.NotNull(extracted, "Ruta extraida");
                    Assert.True(File.Exists(extracted), "Archivo extraido existe");
                    Assert.True(BytesEqual(bytes, File.ReadAllBytes(extracted)), "Bytes extraidos");
                }
                finally
                {
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                    if (extracted != null && File.Exists(extracted)) File.Delete(extracted);
                }
            }));

            // ======================= HRTF / SPATIAL AUDIO =======================
            tests.Add(Tuple.Create<string, Action>("HRTF: columna A pan izquierdo completo (-0.85)", () =>
            {
                Assert.Near(-SpatialAudio.MaxPan, SpatialAudio.PanColumn(0), 0.0001f, "Pan col 0");
            }));

            tests.Add(Tuple.Create<string, Action>("HRTF: columna H pan derecho completo (+0.85)", () =>
            {
                Assert.Near(SpatialAudio.MaxPan, SpatialAudio.PanColumn(7), 0.0001f, "Pan col 7");
            }));

            tests.Add(Tuple.Create<string, Action>("HRTF: centro entre columnas 3 y 4", () =>
            {
                Assert.True(SpatialAudio.PanColumn(3) < 0.0f, "Col 3 debe ser izquierda");
                Assert.True(SpatialAudio.PanColumn(4) > 0.0f, "Col 4 debe ser derecha");
                Assert.True(Math.Abs(SpatialAudio.PanColumn(3)) < 0.5f, "Col 3 cercano al centro");
                Assert.True(Math.Abs(SpatialAudio.PanColumn(4)) < 0.5f, "Col 4 cercano al centro");
            }));

            tests.Add(Tuple.Create<string, Action>("HRTF: curva monotona creciente A->H", () =>
            {
                for (int i = 0; i < 7; i++)
                    Assert.True(SpatialAudio.PanColumn(i) < SpatialAudio.PanColumn(i + 1), "pan(" + i + ") < pan(" + (i + 1) + ")");
            }));

            tests.Add(Tuple.Create<string, Action>("HRTF: curva simetrica izquierda/derecha", () =>
            {
                Assert.Near(Math.Abs(SpatialAudio.PanColumn(0)), Math.Abs(SpatialAudio.PanColumn(7)), 0.001f, "Extremos");
                Assert.Near(Math.Abs(SpatialAudio.PanColumn(1)), Math.Abs(SpatialAudio.PanColumn(6)), 0.001f, "Par 1/6");
                Assert.Near(Math.Abs(SpatialAudio.PanColumn(2)), Math.Abs(SpatialAudio.PanColumn(5)), 0.001f, "Par 2/5");
                Assert.Near(Math.Abs(SpatialAudio.PanColumn(3)), Math.Abs(SpatialAudio.PanColumn(4)), 0.001f, "Par 3/4");
            }));

            tests.Add(Tuple.Create<string, Action>("HRTF: sin columna (UI/menus) queda al centro (regresion #bug izquierda)", () =>
            {
                Assert.Near(0.0f, SpatialAudio.PanColumn(-1), 0.0001f, "col=-1 UI al centro");
                Assert.Near(0.0f, SpatialAudio.Pan(-1, 8), 0.0001f, "Pan(-1,8) al centro");
                Assert.True(SpatialAudio.PanColumn(-1) != SpatialAudio.PanColumn(0), "No debe volcarse a izquierda");
            }));

            tests.Add(Tuple.Create<string, Action>("HRTF: valores fuera de rango van al centro", () =>
            {
                Assert.Near(0.0f, SpatialAudio.PanColumn(-5), 0.0001f, "Izquierda fuera");
                Assert.Near(0.0f, SpatialAudio.PanColumn(99), 0.0001f, "Derecha fuera");
                Assert.Near(0.0f, SpatialAudio.PanColumn(Board.Cols), 0.0001f, "Primera col fuera del tablero");
            }));

            tests.Add(Tuple.Create<string, Action>("HRTF: swipe anima el pan de una columna a otra", () =>
            {
                float fromPan = SpatialAudio.PanColumn(0);
                float toPan = SpatialAudio.PanColumn(7);
                // Primitive: a lone displaced near-mid progress must be between.
                float prog = 0.5f;
                float mid = SpatialAudio.SweepPan(fromPan, toPan, prog);
                Assert.True(mid > fromPan && mid < toPan, "El punto medio del swipe debe estar entre A y H");
                Assert.Near(toPan, SpatialAudio.SweepPan(fromPan, toPan, 1.0f), 0.0001f, "Al 100% debe llegar a H");
                Assert.Near(fromPan, SpatialAudio.SweepPan(fromPan, toPan, 0.0f), 0.0001f, "Al 0% debe partir de A");
            }));

            tests.Add(Tuple.Create<string, Action>("HRTF: la voz siempre se reproduce al centro", () =>
            {
                Assert.Near(0.0f, SpatialAudio.VoicePan, 0.0001f, "VoicePan constante = centro");
            }));

            tests.Add(Tuple.Create<string, Action>("HRTF: profundidad monotona fondo->frente", () =>
            {
                Assert.Near(0.0f, SpatialAudio.Depth(0, 8), 0.0001f, "Fila 1 = fondo");
                Assert.Near(1.0f, SpatialAudio.Depth(7, 8), 0.0001f, "Fila 8 = frente");
                Assert.True(SpatialAudio.Depth(3, 8) < SpatialAudio.Depth(4, 8), "Profundidad creciente");
                Assert.True(SpatialAudio.Depth(0, 8) < SpatialAudio.Depth(7, 8), "Fondo != frente");
            }));

            tests.Add(Tuple.Create<string, Action>("HRTF: filas lejanas mas bajas, estrechas y oscuras", () =>
            {
                Assert.Near(0.80f, SpatialAudio.DepthVolumeForRow(0), 0.001f, "Volumen fondo");
                Assert.Near(1.00f, SpatialAudio.DepthVolumeForRow(7), 0.001f, "Volumen frente");
                Assert.Near(0.75f, SpatialAudio.DepthPanScaleForRow(0), 0.001f, "Pan fondo estrecho");
                Assert.Near(1.00f, SpatialAudio.DepthPanScaleForRow(7), 0.001f, "Pan frente pleno");
                Assert.Near(0.965f, SpatialAudio.DepthPitchForRow(0), 0.001f, "Pitch fondo grave");
                Assert.Near(1.000f, SpatialAudio.DepthPitchForRow(7), 0.001f, "Pitch frente natural");
                Assert.True(SpatialAudio.DepthVolumeForRow(1) < SpatialAudio.DepthVolumeForRow(5), "Volumen crece hacia abajo");
            }));

            tests.Add(Tuple.Create<string, Action>("HRTF: sin fila (UI) plana y al centro", () =>
            {
                Assert.Near(1.0f, SpatialAudio.DepthVolumeForRow(-1), 0.0001f, "UI sin atenuar");
                Assert.Near(1.0f, SpatialAudio.DepthPanScaleForRow(-1), 0.0001f, "UI pan sin estrechar");
                Assert.Near(1.0f, SpatialAudio.DepthPitchForRow(-1), 0.0001f, "UI pitch natural");
                Assert.Near(0.0f, SpatialAudio.PanAt(-1, 4, 8), 0.0001f, "col=-1 al centro");
                Assert.Near(SpatialAudio.PanColumn(3), SpatialAudio.PanAt(3, 7, 8), 0.001f, "Frente = pan pleno");
            }));

            tests.Add(Tuple.Create<string, Action>("HRTF: pan plegado por profundidad", () =>
            {
                Assert.Near(0.6375f, Math.Abs(SpatialAudio.PanAt(0, 0, 8)), 0.001f, "Fondo col A = 0.85*0.75");
                Assert.Near(0.8500f, Math.Abs(SpatialAudio.PanAt(0, 7, 8)), 0.001f, "Frente col A = 0.85");
                Assert.True(Math.Abs(SpatialAudio.PanAt(0, 0, 8)) < Math.Abs(SpatialAudio.PanAt(0, 7, 8)), "Fondo mas centrado que frente");
                Assert.True(Math.Abs(SpatialAudio.PanAt(3, 0, 8)) < Math.Abs(SpatialAudio.PanAt(3, 7, 8)), "Centro plegado simetrico");
            }));

            tests.Add(Tuple.Create<string, Action>("HRTF: el glide cruza por delante (bulge)", () =>
            {
                Assert.Near(1.0f, SpatialAudio.SweepPassBulge(0.0f), 0.0001f, "Inicio plano");
                Assert.Near(1.0f, SpatialAudio.SweepPassBulge(1.0f), 0.0001f, "Fin plano");
                Assert.Near(1.1f, SpatialAudio.SweepPassBulge(0.5f), 0.001f, "Cima al cruzar el centro");
                Assert.True(SpatialAudio.SweepPassBulge(0.25f) > 1.0f, "Crece hacia el centro");
                Assert.True(SpatialAudio.SweepPassBulge(0.5f) > SpatialAudio.SweepPassBulge(0.9f), "Decae tras el centro");
            }));

            tests.Add(Tuple.Create<string, Action>("Sound: valores por defecto del motor", () =>
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                using (SoundEngine sound = new SoundEngine(baseDir))
                {
                    Assert.Equal(80, sound.MusicVol, "Musica");
                    Assert.Equal(100, sound.SfxVol, "Sonido");
                    Assert.Equal(100, sound.VoiceVol, "Voz");
                    Assert.True(sound.SpatialBinauralEnabled, "HRTF activado por defecto");
                }
            }));

            tests.Add(Tuple.Create<string, Action>("Sound: reproduccion spatial (HRTF) sin excepciones", () =>
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                using (SoundEngine sound = new SoundEngine(baseDir))
                {
                    Assert.NoThrow(() => sound.PlaySoundSpatial("select", 0, 0), "Play col 0");
                    Assert.NoThrow(() => sound.PlaySoundSpatial("select", 7, 7), "Play col 7");
                    System.Threading.Thread.Sleep(150);
                }
            }));

            tests.Add(Tuple.Create<string, Action>("Sound: reproduccion pitch y musica sin excepciones", () =>
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                using (SoundEngine sound = new SoundEngine(baseDir))
                {
                    Assert.NoThrow(() => sound.PlaySoundPitch("button_press", 1.2f), "Play pitch");
                    Assert.NoThrow(() => sound.PlayMusic("01 - Intro.mp3"), "Play music");
                    System.Threading.Thread.Sleep(150);
                    Assert.NoThrow(() => sound.StopMusic(), "Stop music");
                }
            }));

            tests.Add(Tuple.Create<string, Action>("Sound: crossfade de musica no se auto-bloquea (regresion #11)", () =>
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                using (SoundEngine sound = new SoundEngine(baseDir))
                {
                    sound.MusicVol = 100;
                    sound.PlayMusic("01 - Intro.mp3");

                    // Esperar a que el fade-in termine (~20 pasos * 25 ms). El bug
                    // colgaba el hilo del timer al auto-cancelarse desde su propio
                    // callback, bloqueando _musicLock para siempre.
                    System.Threading.Thread.Sleep(900);

                    // Despues del fade-in completado, cada corte/arrancada de musica
                    // (y cada key=>TransitionToMainMenu) llamaria PlayMusic/StopMusic.
                    // Si el timer se colgo, PlayMusic se quedaria bloqueado aqui.
                    bool done = false;
                    System.Threading.Thread worker = new System.Threading.Thread(delegate ()
                    {
                        sound.PlayMusic("02 - Bejeweled 3 Theme.mp3");
                        done = true;
                    });
                    worker.IsBackground = true;
                    worker.Start();

                    bool finished = worker.Join(4000);
                    Assert.True(finished, "PlayMusic tras el fade-in no debe bloquearse (crossfade)");
                    System.Threading.Thread.Sleep(700); // dejar terminar el crossfade out
                    Assert.NoThrow(() => sound.StopMusic(), "Stop music tras crossfade");
                }
            }));

            tests.Add(Tuple.Create<string, Action>("Sound: encadenado sigue armado tras un crossfade (regresion #loops)", () =>
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                using (SoundEngine sound = new SoundEngine(baseDir))
                {
                    sound.MusicVol = 100;
                    sound.PlayMusic("01 - Intro.mp3");

                    // Fade-in completado (~8 pasos * 25 ms)
                    System.Threading.Thread.Sleep(900);

                    // Crossfade a otra pista: el bug dejaba _fadingOut=true
                    // para siempre y congelaba el monitor de encadenado.
                    sound.PlayMusic("02 - Bejeweled 3 Theme.mp3");

                    // Esperar a que el crossfade out termine y se re-arme.
                    System.Threading.Thread.Sleep(700);

                    Assert.True(sound.MusicLoopArmed, "El monitor de encadenado debe seguir operativo tras el crossfade");
                    Assert.NoThrow(() => sound.StopMusic(), "Stop music tras crossfade");
                }
            }));

            tests.Add(Tuple.Create<string, Action>("Sound: reencadenado real al final de pista sin cortes (regresion #vacio)", () =>
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                using (SoundEngine sound = new SoundEngine(baseDir))
                {
                    sound.MusicVol = 100;
                    bool rechained = false;
                    sound.MusicRechained += delegate { rechained = true; };
                    sound.PlayMusic("01 - Intro.mp3");

                    // Reproducir la pista completa (intro recortada ~26 s) y exigir
                    // que el reencadenado se dispare ANTES del final, sin que el
                    // canal quede inactivo un solo instante (el bug #vacio dejaba
                    // la cola de silencio del MP3 a volumen cero durante segundos).
                    bool stayedActive = true;
                    int waited = 0;
                    while (waited < 35000 && !rechained)
                    {
                        System.Threading.Thread.Sleep(250);
                        waited += 250;
                        if (!sound.MusicChannelActive) stayedActive = false;
                    }
                    System.Threading.Thread.Sleep(800);
                    Assert.True(rechained, "La pista debe reencadenarse al llegar a su final");
                    Assert.True(stayedActive, "El canal de musica nunca debe quedar inactivo (sin vacio)");
                    Assert.True(sound.MusicChannelActive, "Musica reproduciendose tras el reencadenado");
                    Assert.True(sound.MusicLoopArmed, "El monitor debe re-armarse tras el reencadenado");
                    Assert.NoThrow(() => sound.StopMusic(), "Stop music");
                }
            }));

            tests.Add(Tuple.Create<string, Action>("Sound: rafaga de voces se encola sin cortes ni excepciones", () =>
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                using (SoundEngine sound = new SoundEngine(baseDir))
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Assert.NoThrow(() => sound.PlaySound("voice_awesome"), "Voz encolada " + i);
                    }
                    System.Threading.Thread.Sleep(300);
                    Assert.NoThrow(() => sound.StopActiveVoices(), "Limpieza de cola");
                }
            }));

            tests.Add(Tuple.Create<string, Action>("Sound: voz espacial por columna se encola sin excepciones", () =>
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                using (SoundEngine sound = new SoundEngine(baseDir))
                {
                    Assert.NoThrow(() => sound.PlaySoundSpatial("voice_gameover", 0, 0), "Voz col A");
                    Assert.NoThrow(() => sound.PlaySoundSpatial("voice_gameover", 7, 3), "Voz col H");
                    System.Threading.Thread.Sleep(250);
                }
            }));

            tests.Add(Tuple.Create<string, Action>("Sound: VoiceVol=0 bloquea la cola de voces pero no los SFX", () =>
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                using (SoundEngine sound = new SoundEngine(baseDir))
                {
                    sound.VoiceVol = 0;
                    sound.SfxVol = 100;
                    sound.PlaySound("voice_awesome");
                    Assert.Equal(0, sound.VoicePendingCount, "VoiceVol=0 no debe encolar voces");
                    Assert.NoThrow(() => sound.PlaySound("select"), "Los SFX deben seguir sonando");
                    System.Threading.Thread.Sleep(100);
                }
            }));

            tests.Add(Tuple.Create<string, Action>("Sound: SfxVol=0 no bloquea la cola de voces", () =>
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                using (SoundEngine sound = new SoundEngine(baseDir))
                {
                    sound.SfxVol = 0;
                    sound.VoiceVol = 100;
                    sound.PlaySound("voice_awesome");
                    Assert.Equal(1, sound.VoicePendingCount, "Las voces deben encolarse aunque SfxVol sea 0");
                    sound.StopActiveVoices();
                }
            }));

            // Reproduccion real de voces simulando una rafaga de eventos de juego:
            // verifica que ninguna locucion se corta (dura su duracion completa,
            // llega a su final natural) ni se solapa con la siguiente.
            tests.Add(Tuple.Create<string, Action>("Sound: rafaga de voces NO se interrumpe ni se solapa (audio real)", () =>
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                using (SoundEngine sound = new SoundEngine(baseDir))
                {
                    string[] burst = { "voice_good", "voice_excellent", "voice_awesome", "voice_spectacular", "voice_levelcomplete", "voice_gameover" };
                    foreach (string v in burst) sound.PlaySound(v);

                    int waited = 0;
                    while (sound.IsVoiceBusy && waited < 15000) { System.Threading.Thread.Sleep(25); waited += 25; }

                    // IsVoiceBusy pasa a false en cuanto la ultima voz alcanza su endAt, pero el
                    // pump puede tardar hasta VOICE_PUMP_MS en finalizarla y registrarla: esperamos
                    // que el historial alcance el total antes de leerlo.
                    while (sound.GetVoicePlaybackHistory().Length < burst.Length && waited < 15000)
                    {
                        System.Threading.Thread.Sleep(40);
                        waited += 40;
                    }

                    SoundEngine.VoicePlayback[] log = sound.GetVoicePlaybackHistory();
                    string dbg = "[audio-voice-test] pendientes=" + sound.VoicePendingCount + " error='" + sound.VoiceLastError + "' trace='" + sound.VoiceTrace + "' " + string.Join(" | ",
                        Array.ConvertAll(log, l => l.SoundName + " dur=" + l.DurationMs + " len=" + l.LengthBytes + " played=" + l.PlayedBytes));
                    Console.WriteLine(dbg);
                    Assert.Equal(burst.Length, log.Length,
                        "Todas las voces encoladas deben reproducirse completas y registrarse (sin cortes)");

                    // 1) Ninguna voz se corto: llego a su final natural
                    for (int i = 0; i < log.Length; i++)
                    {
                        Assert.True(log[i].FullyPlayed, "Voz '" + log[i].SoundName + "' se interrumpio antes de terminar");
                        Assert.True(log[i].DurationMs > 0, "Duracion no valida para '" + log[i].SoundName + "'");
                    }

                    // 2) Sin solapamientos: cada voz empieza cuando la anterior ya termino (+ margen)
                    for (int i = 1; i < log.Length; i++)
                    {
                        Assert.True(log[i].StartMs >= log[i - 1].EndMs - 50,
                            "Solapamiento entre '" + log[i - 1].SoundName + "' y '" + log[i].SoundName + "'");
                    }

                    // 3) El plano temporal coincide con la suma real de duraciones + espacios
                    //    (si el planificador hubiera cortado alguna voz, el espacio quedaria corto)
                    long expectedSpan = 0;
                    for (int i = 0; i < log.Length; i++) expectedSpan += (log[i].EndMs - log[i].StartMs);
                    long actualSpan = log[log.Length - 1].EndMs - log[0].StartMs;
                    Assert.True(actualSpan >= expectedSpan - 60,
                        "El planificador termino antes de tiempo (pudo cortar una voz)");
                    Assert.True(actualSpan <= expectedSpan + 1000,
                        "El planificador quedo atascado esperando un silencio excesivo");
                }
            }));

            // ======================= ZEN MANAGER =======================
            tests.Add(Tuple.Create<string, Action>("Zen: pista por nivel (Partes 1-4 ciclicas)", () =>
            {
                Assert.Equal("11 - Zen - Part 1.mp3", ZenManager.GetZenTrackForLevel(1), "Nivel 1");
                Assert.Equal("12 - Zen - Part 2 - Schein Zwei.mp3", ZenManager.GetZenTrackForLevel(2), "Nivel 2");
                Assert.Equal("13 - Zen - Part 3 - The Return.mp3", ZenManager.GetZenTrackForLevel(3), "Nivel 3");
                Assert.Equal("14 - Zen - Part 4.mp3", ZenManager.GetZenTrackForLevel(4), "Nivel 4");
                Assert.Equal("11 - Zen - Part 1.mp3", ZenManager.GetZenTrackForLevel(5), "Nivel 5 ciclico");
                Assert.Equal("11 - Zen - Part 1.mp3", ZenManager.GetZenTrackForLevel(0), "Nivel 0");
            }));

            tests.Add(Tuple.Create<string, Action>("Zen: mapeo de pistas ambientales", () =>
            {
                Assert.Equal("24 - Coastal.mp3", ZenManager.GetAmbientTrack(AmbientType.Coastal), "Coastal");
                Assert.Equal("25 - Crickets.mp3", ZenManager.GetAmbientTrack(AmbientType.Crickets), "Crickets");
                Assert.Equal("26 - Forest.mp3", ZenManager.GetAmbientTrack(AmbientType.Forest), "Forest");
                Assert.Equal("27 - Ocean Surf.mp3", ZenManager.GetAmbientTrack(AmbientType.OceanSurf), "OceanSurf");
                Assert.Equal("28 - Rain Leaves.mp3", ZenManager.GetAmbientTrack(AmbientType.RainLeaves), "RainLeaves");
                Assert.Equal("29 - Waterfall.mp3", ZenManager.GetAmbientTrack(AmbientType.Waterfall), "Waterfall");
                Assert.Equal("", ZenManager.GetAmbientTrack(AmbientType.None), "None");
            }));

            tests.Add(Tuple.Create<string, Action>("Zen: nombres de ambientes se localizan en espanol", () =>
            {
                Localization.CurrentLanguage = Language.Spanish;
                Assert.Equal("Costa", ZenManager.GetAmbientName(AmbientType.Coastal), "Coastal ES");
                Assert.Equal("Grillos", ZenManager.GetAmbientName(AmbientType.Crickets), "Crickets ES");
                Assert.Equal("Bosque", ZenManager.GetAmbientName(AmbientType.Forest), "Forest ES");
                Assert.Equal("Olas del Mar", ZenManager.GetAmbientName(AmbientType.OceanSurf), "OceanSurf ES");
                Assert.Equal("Lluvia y Hojas", ZenManager.GetAmbientName(AmbientType.RainLeaves), "RainLeaves ES");
                Assert.Equal("Cascada", ZenManager.GetAmbientName(AmbientType.Waterfall), "Waterfall ES");
                Assert.Equal("Ninguno", ZenManager.GetAmbientName(AmbientType.None), "None ES");
            }));

            tests.Add(Tuple.Create<string, Action>("Zen: nombres de ambientes en ingles difieren", () =>
            {
                Localization.CurrentLanguage = Language.English;
                Assert.Equal("Coastal", ZenManager.GetAmbientName(AmbientType.Coastal), "Coastal EN");
                Assert.Equal("Crickets", ZenManager.GetAmbientName(AmbientType.Crickets), "Crickets EN");
                Assert.Equal("Forest", ZenManager.GetAmbientName(AmbientType.Forest), "Forest EN");
                Assert.Equal("Ocean Surf", ZenManager.GetAmbientName(AmbientType.OceanSurf), "OceanSurf EN");
                Assert.Equal("Rain Leaves", ZenManager.GetAmbientName(AmbientType.RainLeaves), "RainLeaves EN");
                Assert.Equal("Waterfall", ZenManager.GetAmbientName(AmbientType.Waterfall), "Waterfall EN");
                Assert.Equal("None", ZenManager.GetAmbientName(AmbientType.None), "None EN");
            }));

            tests.Add(Tuple.Create<string, Action>("Zen: banco de afirmaciones completo del original", () =>
            {
                Assert.Equal(49, Affirmations.ThemeCount(AffirmationTheme.General), "General");
                Assert.Equal(57, Affirmations.ThemeCount(AffirmationTheme.PositiveThinking), "PositiveThinking");
                Assert.Equal(47, Affirmations.ThemeCount(AffirmationTheme.Prosperity), "Prosperity");
                Assert.Equal(36, Affirmations.ThemeCount(AffirmationTheme.QuitBadHabits), "QuitBadHabits");
                Assert.Equal(36, Affirmations.ThemeCount(AffirmationTheme.SelfConfidence), "SelfConfidence");
                Assert.Equal(40, Affirmations.ThemeCount(AffirmationTheme.WeightLoss), "WeightLoss");
                Assert.Equal(265, Affirmations.TotalCount(), "Total del banco");
            }));

            tests.Add(Tuple.Create<string, Action>("Zen: todas las afirmaciones traducidas y no vacias", () =>
            {
                Localization.CurrentLanguage = Language.Spanish;
                foreach (AffirmationTheme theme in Enum.GetValues(typeof(AffirmationTheme)))
                {
                    for (int i = 0; i < Affirmations.ThemeCount(theme); i++)
                    {
                        string es = Affirmations.Get(theme, i);
                        Assert.True(!string.IsNullOrEmpty(es) && es != "", "Frase ES vacia en " + theme + " #" + i);
                    }
                }
                Localization.CurrentLanguage = Language.English;
                foreach (AffirmationTheme theme in Enum.GetValues(typeof(AffirmationTheme)))
                {
                    for (int i = 0; i < Affirmations.ThemeCount(theme); i++)
                    {
                        string en = Affirmations.Get(theme, i);
                        Assert.True(!string.IsNullOrEmpty(en) && en != "", "Frase EN vacia en " + theme + " #" + i);
                    }
                }
            }));

            tests.Add(Tuple.Create<string, Action>("Zen: ciclo de mantras no repite hasta agotar el banco", () =>
            {
                List<Tuple<AffirmationTheme, int>> order = Affirmations.BuildOrder(new Random(42));
                Assert.Equal(265, order.Count, "El orden cubre todo el banco");
                HashSet<string> seen = new HashSet<string>();
                for (int i = 0; i < order.Count; i++)
                {
                    string key = order[i].Item1 + ":" + order[i].Item2;
                    Assert.True(!seen.Contains(key), "Repeticion en " + key + " en la posicion " + i);
                    seen.Add(key);
                }
                Assert.Equal(265, seen.Count, "Sin duplicados");
            }));

            tests.Add(Tuple.Create<string, Action>("Zen: frases originales se conservan", () =>
            {
                Localization.CurrentLanguage = Language.English;
                Assert.Equal("I let fear pass me by.", Affirmations.Get(AffirmationTheme.General, 0), "General 0");
                Assert.Equal("I deserve abundance.", Affirmations.Get(AffirmationTheme.Prosperity, 0), "Prosperity 0");
                Assert.Equal("I am a winner.", Affirmations.Get(AffirmationTheme.QuitBadHabits, 24), "QuitBadHabits 24");
                Assert.Equal("I take action.", Affirmations.Get(AffirmationTheme.SelfConfidence, 31), "SelfConfidence 31");
                Assert.Equal("My body is perfect right now.", Affirmations.Get(AffirmationTheme.WeightLoss, 0), "WeightLoss 0");
            }));

            // ======================= ACCESSIBILITY =======================
            tests.Add(Tuple.Create<string, Action>("NVDA/SAPI: construir, hablar y liberar sin excepciones", () =>
            {
                Assert.NoThrow(() =>
                {
                    using (NvdaSpeech speech = new NvdaSpeech())
                    {
                        speech.Speak("Prueba de auditoria de accesibilidad", true);
                    }
                }, "NvdaSpeech ciclo de vida");
            }));

            return tests;
        }

        // ======================= HELPERS =======================

        private static void FillCheckerboard(Board board, GemColor c1 = GemColor.Purple, GemColor c2 = GemColor.Blue)
        {
            for (int y = 0; y < Board.Rows; y++)
            {
                for (int x = 0; x < Board.Cols; x++)
                {
                    board.SetGem(x, y, new Gem(((x + y) % 2 == 0) ? c1 : c2));
                }
            }
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private static void RunHrtfScan()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            Console.WriteLine("=== ESCANEO HRTF (tablero 8x8): columna A (izquierda) a H (derecha) ===");
            using (SoundEngine sound = new SoundEngine(baseDir))
            {
                for (int col = 0; col < 8; col++)
                {
                    Console.WriteLine(string.Format("Columna {0}: pan = {1:F3}", (char)('A' + col), SpatialAudio.PanColumn(col)));
                    sound.PlaySoundSpatial("select", col, 3);
                    System.Threading.Thread.Sleep(350);
                }
                System.Threading.Thread.Sleep(600);
            }
            Console.WriteLine("Escaneo HRTF completado. Deberias escuchar el sonido moverse de izquierda a derecha.");
        }
    }
}
