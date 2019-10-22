using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection.Emit;
using NUnit.Framework.Api;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Editor.MineSweeper
{

    public class Cell
    {
        public bool IsBomb;
        public int AroundBombs;

        public bool IsOpened;

        /// <summary>
        /// 0 -> natural
        /// 1 -> flag
        /// 2 -> question
        /// </summary>
        public int Status;

        public bool IsFlag => Status == 1;
    }
    public class Window : EditorWindow
    {
        private int width = 9;
        private int height = 9;
        private int bombs = 9;

        private const int margin = 10;

        private const int cellSize = 50;
        private const int headerSize = 120;

        private Cell[,] cells;

        private GUIStyle[] styles ;
        private GUIStyle countStyle;

        private Texture bombImage;
        private Texture[] statusImages;
        private Texture[] gameStatusImages;

        private bool StartedGame
        {
            set { GameStatus = value ? 1 : 0; }
            get => GameStatus == 1;
        }

        /// <summary>
        /// 0 -> NotStarted
        /// 1 -> Gaming
        /// 2 -> WinResult
        /// 3 -> LoseResult
        /// </summary>
        private int GameStatus;

        private int windowWidth;
        private int windowHeight;



        [MenuItem("MineSweeper/Open",priority = 0)]
        public static void Create()
        {
            var w = EditorWindow.GetWindow<Window>("MineSweeper");
            w.Initialized();


        }

        [MenuItem("MineSweeper/CreateSetting", priority = 100)]
        public static void CreateSetting()
        {
            AssetDatabase.CreateAsset(CreateInstance<GameSettings>(), "Assets/Resources/NewSettings.asset");
        }
        private void SetSize()
        {
            maxSize = minSize = new Vector2(windowWidth = width * cellSize +  margin * 2,windowHeight = height * cellSize + margin * 2 + headerSize);
        }

        private Texture2D countBack;
        private Texture2D MakeTexture(Color col)
        {
            Color[] pix = new Color[10 * 10];

            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }


        private void Initialized()
        {





            countBack = countBack ?? MakeTexture(Color.black);
            countStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState()
                {
                    textColor = Color.yellow,
                    background = countBack
                },
                
                fontSize = 50
            };

            //images
            bombImage = Resources.Load<Texture>("bomb");
            var questionImage = Resources.Load<Texture>("question");
            var flagImage = Resources.Load<Texture>("flag");

            statusImages = new Texture[]
            {
                new Texture2D(0,0),
                flagImage,
                questionImage
            };

            gameStatusImages = new Texture[]
            {
                Resources.Load<Texture>("gaming"),
                Resources.Load<Texture>("gaming"),
                Resources.Load<Texture>("smile"),
                Resources.Load<Texture>("gameover")

            };

            var d = PlayerPrefs.GetString("difficultly") ?? "easy";
            titleContent = new GUIContent("MineSweeper - " + d);

            var settings = Resources.Load<GameSettings>(d);

            width = settings.Width;
            height = settings.Height;
            bombs = settings.Bombs;

            SetSize();
            SetStartBoard();
        }

        void SetStartBoard()
        {
            var size = width * height;
            cells = new Cell[width, height];

            for (int i = 0; i < size; i++)
            {
                cells[i % width, i / width] = new Cell();
            }

            StartedGame = false;
        }

        void CreateBoard(int fx,int fy)
        {
            //initialize the board
            var size = width * height;
            cells = new Cell[width, height];

            for (int i = 0; i < size; i++)
            {
                cells[i % width, i / width] = new Cell()
                {
                    IsBomb = i < bombs
                };
            }

            void Swap(int a, int b)
            {
                if (a == b) return;

                var a1 = a % width;
                var a2 = a / width;

                var b1 = b % width;
                var b2 = b / width;

                var t = cells[a1, a2];
                cells[a1, a2] = cells[b1, b2];
                cells[b1, b2] = t;
            }

            // random sort without last cell
            for (int i = 0; i < size; i++)
            {
                Swap(i, UnityEngine.Random.Range(i, size - 2));
            }

            // swap 1st clicked cell and last cell
            Swap(fy * width + fx,size -1);

            int Check(int x, int y)
            {

                if (x < 0 || y < 0 || x >= width || y >= height) return 0;
                return cells[x, y].IsBomb ? 1 : 0;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    cells[x, y].AroundBombs =
                        Check(x - 1, y - 1) + Check(x - 1, y) + Check(x - 1, y + 1) +
                        Check(x, y - 1) + Check(x, y + 1) +
                        Check(x + 1, y - 1) + Check(x + 1, y) + Check(x + 1, y + 1);
                }
            }
        }

        int CheckFlag(int x, int y)
        {

            if (x < 0 || y < 0 || x >= width || y >= height) return 0;
            return cells[x, y].IsFlag ? 1 : 0;
        }

        void OnGUI()
        {

            if (styles == null)
            {
                //create fonts
                Color[] color = new Color[]
                {
                    Color.clear,
                    Color.blue,
                    Color.green,
                    new Color(0.1f,0.3f,0.4f),
                    new Color(0.5f,0.25f,0.12f),
                    Color.magenta,
                    Color.red,
                    new Color(0.6f,0f,0.8f),
                    Color.black,
                };

                styles = new GUIStyle[9];
                for (int i = 0; i < color.Length; i++)
                {
                    styles[i] = new GUIStyle(GUI.skin.box)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 20
                    };
                    styles[i].normal.textColor = color[i];
                }
                
            }

            var ir = new Rect(windowWidth / 2 - 37,10 , 74,100);
            if (GameStatus >= 2)
            {
                if (GUI.Button(ir, gameStatusImages[GameStatus]))
                {
                    SetStartBoard();
                }
            }
            else
            {
                GUI.Label(ir, gameStatusImages[GameStatus], GUI.skin.box);
            }


            if (cells == null) return;

            GUI.Label( new Rect(windowWidth / 2 + 50, 10, 100, 100), (bombs - cells.OfType<Cell>().Count(c => c.IsFlag)).ToString(),countStyle);

            var e = Event.current;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var cellRect = new Rect(x * cellSize + margin,y * cellSize + margin + headerSize,cellSize,cellSize);
                    var c = cells[x, y];

                    if (!c.IsOpened)
                    {

                        var bc = GUI.backgroundColor;
                        GUI.backgroundColor = Color.gray;
                        //when push the button, open it
                        if (GUI.Button(cellRect, statusImages[c.Status]))
                        {
                            if (e.type == EventType.Used && e.button == 0)
                            {
                                //Start the game
                                if (!c.IsFlag && !StartedGame)
                                {
                                    CreateBoard(x, y);
                                    StartedGame = true;
                                }

                                if (!c.IsFlag)
                                {
                                    if (Open(x, y))
                                    {
                                        CheckWin();
                                    }
                                }
                            }else if (e.type == EventType.Used && e.button == 1) // set a flag
                            {
                                c.Status = (c.Status + 1) % 3;
                            }

                        }
                        GUI.backgroundColor = bc;
                    }
                    else if (c.IsBomb)
                    {
                        GUI.Label(cellRect, bombImage,styles[0]);
                    }
                    else
                    {
                        // open around cell
                        if (e.type == EventType.MouseUp && e.button == 2 && cellRect.Contains(e.mousePosition))
                        {
                            var aflags = CheckFlag(x - 1, y - 1) + CheckFlag(x - 1, y) + CheckFlag(x - 1, y + 1) +
                                         CheckFlag(x, y - 1) + CheckFlag(x, y + 1) +
                                         CheckFlag(x + 1, y - 1) + CheckFlag(x + 1, y) + CheckFlag(x + 1, y + 1);
                            if (aflags == c.AroundBombs)
                            {
                                if (OpenAround(x, y))
                                {
                                    CheckWin();
                                }
                            }
                            
                        }

                        GUI.Label(cellRect, cells[x, y].AroundBombs.ToString(), styles[cells[x, y].AroundBombs]);
                    }

                }
            }
            Repaint();
        }

        bool OpenAround(int x,int y)
        {
            return Open(x - 1, y - 1) &&
            Open(x - 1, y) &&
            Open(x - 1, y + 1) &&
            Open(x, y - 1) &&
            Open(x, y + 1) &&
            Open(x + 1, y - 1) &&
            Open(x + 1, y) &&
            Open(x + 1, y + 1);
        }

        /// <summary>
        /// if the game is over return false, otherwise true. 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        bool Open(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return true;

            var c = cells[x, y];
            if(c.IsOpened) return true;
            if (c.IsFlag && (c.IsBomb || c.AroundBombs != 0)) return true;

            c.IsOpened = true;
            if (c.IsBomb)
            {
                GameOver();
                return false;
            }

            if (c.AroundBombs == 0)
            {
                OpenAround(x,y);
            }

            // else nothing to do 
            return true;
        }

        void CheckWin()
        {
            if (cells.OfType<Cell>().All(ce => ce.IsOpened || (ce.IsBomb && !ce.IsOpened)))
            {
                Win();
            }
        }
        void GameOver()
        {
            EditorUtility.DisplayDialog("Bomb!!", "YOU LOSE!", "OK");
            foreach (var cell in cells)
            {
                cell.IsOpened = true;
            }

            GameStatus = 3;
            Repaint();
        }

        void Win()
        {
            EditorUtility.DisplayDialog("Congratulation!!", "YOU WIN!!", "OK");
            foreach (var cell in cells)
            {
                cell.IsOpened = true;
            }

            GameStatus = 2;
            Repaint();
        }
    }
}
