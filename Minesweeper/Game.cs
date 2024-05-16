using System.Buffers.Text;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Minesweeper
{
    internal class Game
    {
        public int fieldSize;
        public Game()
        {
            Init(true);
        }
        public void Init(bool startAtField)
        {
            string? input;
            if (startAtField)
            {
                Console.Write("Gib die Größe deines Feldes an (max. 20x20): ");
                input = Console.ReadLine();
                if (!int.TryParse(input, out int field))
                {
                    Console.Clear();
                    Console.WriteLine("Die Feldgröße muss numerisch angegeben sein.");
                    Init(true);
                    return;
                }

                if (fieldSize > 20)
                {
                    Console.Clear();
                    Console.WriteLine("Die Feldgröße darf maximal 20 betragen!");
                    Init(true);
                    return;
                }
                fieldSize = field;
            }
            Console.Write("Gib nun die Anzahl der Minen auf deinem Feld an: ");
            input = Console.ReadLine();
            if (!int.TryParse(input, out int mines))
            {
                Console.Clear();
                Console.WriteLine("Die Anzahl der Minen muss numerisch angegeben sein.");
                Init(false);
                return;
            }

            if (mines > fieldSize * fieldSize)
            {
                Console.Clear();
                Console.WriteLine($"Du darfst maximal {fieldSize} Minen haben!");
                Init(false);
                return;
            }
            Game game = new Game(fieldSize, mines);
            game.Start(this);
        }
    }
    internal class Game
    {
        Random rand = new();
        Game minesweeper;
        int fieldSize;
        int mines;
        int[] minePositions;
        int[] discoveredFields;
        char[] alphabet = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
        int[] flags;
        public Game(int fieldSize, int mines)
        {
            this.fieldSize = fieldSize;
            this.mines = mines;
            minePositions = new int[fieldSize * fieldSize];
            discoveredFields = new int[fieldSize * fieldSize];
            flags = new int[fieldSize * fieldSize];
            for (int i = 0; i < discoveredFields.Length; i++)
            {
                discoveredFields[i] = -1;
            }
            for (int i = 0; i < mines; i++)
            {
                int place = rand.Next(fieldSize * fieldSize);
                PlaceMine(place);
            }
        }
        public void Start(Game instance)
        {
            minesweeper = instance;
            Console.Clear();
            Console.Write($"Starte Spiel mit ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(mines);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" Minen auf einem ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{fieldSize}x{fieldSize} ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Feld.");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Nutze \"flag BUCHSTABE:ZAHL\" um eine Flagge zu setzen.");
            Console.WriteLine();
            DrawField();
        }

        public void DrawField()
        {
            Console.Write("  ");
            for (int i = 0; i < fieldSize; i++)
            {
                Console.Write(" " + alphabet[i]);
            }
            Console.WriteLine();
            for (int i = 0; i < fieldSize; ++i)
            {
                string u = "";
                for (int j = 0; j < fieldSize; ++j)
                {
                    int FieldDiscovered = IsFieldDiscovered(i * fieldSize + j);

                    if (FieldDiscovered == -1)
                    {
                        if (flags[i * fieldSize + j] == 0) u += " _";
                        else
                        {
                            u += " ?";
                        }
                    }
                    else
                    {
                        u += " " + FieldDiscovered;
                    }

                }
                if (i <= 8)
                {
                    Console.WriteLine(i + 1 + " " + u);
                }
                else
                {
                    Console.WriteLine(i + 1 + u);
                }
            }
            Console.WriteLine();
            Console.Write("Welches Feld möchtest du aufdecken? (Alphabet:Zahl): ");
            DiscoverField(Console.ReadLine());
        }

        public void PlaceMine(int field)
        {
            if (!IsMineAtField(field))
            {
                minePositions[field] = 1;
                return;
            }
            PlaceMine(rand.Next(fieldSize * fieldSize));
        }

        public bool IsMineAtField(int field)
        {
            return minePositions[field] == 1;
        }
        public int IsFieldDiscovered(int field)
        {
            return discoveredFields[field];
        }
        public int TransformCoordinateToFieldInt(char AlphabetCoordinate, int NumberCoordinate)
        {
            int alphabetIndex = Array.IndexOf(alphabet, char.ToUpper(AlphabetCoordinate));
            if (alphabetIndex == -1)
            {
                Console.WriteLine("Ungültiger Buchstabe für das Alphabet.");
                return -1; // Rückgabe eines ungültigen Werts, um anzuzeigen, dass die Umwandlung fehlgeschlagen ist.
            }

            int numberIndex = NumberCoordinate - 1;
            return (numberIndex * fieldSize) + alphabetIndex;
        }

        public List<int> GetFieldsInRange(int field)
        {
            List<int> result = new List<int>();

            int row = field / fieldSize;
            int col = field % fieldSize;

            int[] relativeRowPositions = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] relativeColPositions = { -1, 0, 1, -1, 1, -1, 0, 1 };

            for (int i = 0; i < 8; i++)
            {
                int newRow = row + relativeRowPositions[i];
                int newCol = col + relativeColPositions[i];

                if (newRow >= 0 && newRow < fieldSize && newCol >= 0 && newCol < fieldSize)
                {
                    int newPosition = newRow * fieldSize + newCol;
                    result.Add(newPosition);
                }
            }

            return result;
        }
        public List<int> GetMinesAroundField(int field)
        {
            List<int> result = new List<int>();
            List<int> fieldsInRange = GetFieldsInRange(field);
            foreach (int position in fieldsInRange)
            {
                if (IsMineAtField(position))
                {
                    result.Add(position);
                }
            }
            return result;
        }
        private async Task UncoverFieldsRecursively(int field)
        {
            if (IsMineAtField(field) || discoveredFields[field] != -1 || IsFlagAtField(field))
                return;

            discoveredFields[field] = GetMinesAroundField(field).Count;

            if (discoveredFields[field] == 0)
            {
                List<int> fieldsInRange = GetFieldsInRange(field);
                foreach (int neighbor in fieldsInRange)
                {
                    await UncoverFieldsRecursively(neighbor);
                }
            }
        }

        public bool IsFlagAtField(int field)
        {
            return flags[field] == 1;
        }
        public int GetSettedFlagsAmount()
        {
            int returnVal = 0;
            foreach (int flag in flags)
            {
                if (flag == 1)
                {
                    returnVal++;
                }
            }
            return returnVal;
        }

        public async void DiscoverField(string? field)
        {
            bool IsFlag = false;
            if (field.Contains("flag"))
            {
                IsFlag = true;
                field = field.Replace("flag", "");
            }
            field = field?.Replace(":", "").Replace(" ", "");
            if (!char.TryParse(field[0].ToString(), out char alphabetChar))
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gib gültige werte an. Bsp.: \"flag D5\", \"d5\"");
                Console.ForegroundColor = ConsoleColor.White;
                DrawField();
                return;
            }
            field = field.Substring(1);
            if (!int.TryParse(field, out int numChar))
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gib eine gültige Zahl an. Bsp.: D:5");
                Console.ForegroundColor = ConsoleColor.White;
                DrawField();
                return;
            }

            if (numChar <= 0 || numChar > fieldSize)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Die Zahl muss zwischen 1 & {fieldSize} liegen.");
                Console.ForegroundColor = ConsoleColor.White;
                DrawField();
                return;
            }
            bool contin = false;
            foreach (char c in alphabet)
            {
                if (alphabetChar.ToString().ToLower().Equals(c.ToString().ToLower())) contin = true;
            }
            if (!contin)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Der Buchstabe muss im aktuellen Spiel vorhanden sein.");
                Console.ForegroundColor = ConsoleColor.White;
                DrawField();
                return;
            }
            Console.Clear();
            int f = TransformCoordinateToFieldInt(alphabetChar, numChar);

            if (IsFlag)
            {
                if (mines - GetSettedFlagsAmount() <= 0 && flags[f] != 1)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Du hast keine Flags mehr.");
                    Console.ForegroundColor = ConsoleColor.White;
                    DrawField();
                    return;
                }
                Console.ForegroundColor = ConsoleColor.Green;
                if (flags[f] == 0)
                {
                    flags[f] = 1;
                    Console.WriteLine($"Flag wurde bei {alphabetChar} {numChar} gesetzt. [{mines - GetSettedFlagsAmount()} verbleibend]");
                    int count = 0;
                    for (int i = 0; i < minePositions.Length; i++)
                    {
                        if (flags[i] == 1 && minePositions[i] == 1)
                        {
                            count++;
                        }
                    }
                    if (count >= mines)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Du hast gewonnen, da du alle Minen geflagt hast!");
                        Console.ForegroundColor = ConsoleColor.White;
                        End();
                        return;
                    }
                }
                else
                {
                    flags[f] = 0;
                    Console.WriteLine($"Flag bei {alphabetChar} {numChar} entfernt. [{mines - GetSettedFlagsAmount()} verbleibend]");
                }
                Console.ForegroundColor = ConsoleColor.White;
                DrawField();
                return;
            }
            if (IsFlagAtField(f))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Du kannst dieses Feld nicht aufdecken, da sich dort eine Flagge befindet. Entferne Sie mit \"flag {alphabetChar}{numChar}\"");
                Console.ForegroundColor = ConsoleColor.White;
                DrawField();
                return;
            }
            if (IsMineAtField(f))
            {
                Console.WriteLine("Du bist auf eine Mine getreten.");
                minesweeper.Init(true);
                return;
            }

            await UncoverFieldsRecursively(f);

            int remaining = 0;

            for (int i = 0; i < discoveredFields.Length; i++)
            {
                if (discoveredFields[i] == -1) remaining++;
            }
            remaining = remaining - mines;
            Console.WriteLine("Feld aufgedeckt: " + alphabetChar + " " + numChar + " [" + remaining + " verbleibend]");
            Console.WriteLine();
            if (remaining <= 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Du hast das Spiel gewonnen, da du alle Felder ohne Minen aufgedeckt hast!");
                Console.ForegroundColor = ConsoleColor.White;
                End();
                return;
            }

            DrawField();
        }
        public void End()
        {
            Console.WriteLine("Das Spiel wurde beendet.");
            minesweeper.Init(true);
        }
    }
}