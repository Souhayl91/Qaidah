#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using SimpleQaidah.Data;
using SimpleQaidah.Core;
using SimpleQaidah.UI;

namespace SimpleQaidah.Editor
{
    public static class ProjectSetup
    {
        private static readonly string BasePath = "Assets/_Project";

        // Arabic alphabet data: letter, name, transliteration
        private static readonly (string arabic, string name, string translit)[] Alphabet = new[]
        {
            ("ا", "Alif", "a"),
            ("ب", "Ba", "b"),
            ("ت", "Ta", "t"),
            ("ث", "Tha", "th"),
            ("ج", "Jeem", "j"),
            ("ح", "Haa", "h"),
            ("خ", "Kha", "kh"),
            ("د", "Daal", "d"),
            ("ذ", "Dhaal", "dh"),
            ("ر", "Ra", "r"),
            ("ز", "Za", "z"),
            ("س", "Seen", "s"),
            ("ش", "Sheen", "sh"),
            ("ص", "Saad", "S"),
            ("ض", "Daad", "D"),
            ("ط", "Taa", "T"),
            ("ظ", "Zaa", "Z"),
            ("ع", "Ain", "'a"),
            ("غ", "Ghain", "gh"),
            ("ف", "Fa", "f"),
            ("ق", "Qaaf", "q"),
            ("ك", "Kaaf", "k"),
            ("ل", "Laam", "l"),
            ("م", "Meem", "m"),
            ("ن", "Noon", "n"),
            ("ه", "Ha", "h"),
            ("و", "Waw", "w"),
            ("ي", "Ya", "y"),
        };

        // Group definitions: (groupName, startIndex, count)
        private static readonly (string name, int start, int count)[] Groups = new[]
        {
            ("Group 1: Alif to Jeem", 0, 5),
            ("Group 2: Haa to Ra", 5, 5),
            ("Group 3: Za to Daad", 10, 5),
            ("Group 4: Taa to Fa", 15, 5),
            ("Group 5: Qaaf to Noon", 20, 5),
            ("Group 6: Ha to Ya", 25, 3),
        };

        [MenuItem("Qaidah/1. Create All Data Assets")]
        public static void CreateDataAssets()
        {
            CreatePlaceholderAudio();
            var letters = CreateLetterAssets();
            var groups = CreateGroupAssets(letters);
            CreateLessonAsset(letters, groups);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("All data assets created successfully!");
        }

        [MenuItem("Qaidah/2. Create All Scenes")]
        public static void CreateAllScenes()
        {
            try { CreateBootScene(); }
            catch (System.Exception e) { Debug.LogError($"BootScene failed: {e}"); }

            try { CreateMainMenuScene(); }
            catch (System.Exception e) { Debug.LogError($"MainMenuScene failed: {e}"); }

            try { CreateLessonScene(); }
            catch (System.Exception e) { Debug.LogError($"LessonScene failed: {e}"); }

            AddScenesToBuildSettings();
            Debug.Log("Scene creation finished. Check console for any errors.");
        }

        [MenuItem("Qaidah/3. Create Prefabs")]
        public static void CreatePrefabs()
        {
            CreateLetterCardPrefab();
            CreateDotPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("All prefabs created successfully!");
        }

        // ─── PLACEHOLDER AUDIO ───────────────────────────────────────

        private static void CreatePlaceholderAudio()
        {
            string audioPath = $"{BasePath}/Audio/Letters";
            EnsureDirectory(audioPath);

            for (int i = 0; i < Alphabet.Length; i++)
            {
                string fileName = $"{Alphabet[i].name.ToLower()}.wav";
                string fullPath = $"{audioPath}/{fileName}";

                if (AssetDatabase.LoadAssetAtPath<AudioClip>(fullPath) != null)
                    continue;

                // Create a minimal silent WAV file (44 bytes header + 8820 samples = 0.1s at 44100Hz mono 16-bit)
                CreateSilentWav(Path.Combine(Application.dataPath, "_Project/Audio/Letters", fileName), 0.1f);
            }

            AssetDatabase.Refresh();
        }

        private static void CreateSilentWav(string absolutePath, float durationSeconds)
        {
            int sampleRate = 44100;
            int channels = 1;
            int bitsPerSample = 16;
            int numSamples = (int)(sampleRate * durationSeconds);
            int dataSize = numSamples * channels * (bitsPerSample / 8);

            using (var stream = new FileStream(absolutePath, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                // RIFF header
                writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataSize);
                writer.Write(new char[] { 'W', 'A', 'V', 'E' });

                // fmt chunk
                writer.Write(new char[] { 'f', 'm', 't', ' ' });
                writer.Write(16); // chunk size
                writer.Write((short)1); // PCM format
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * bitsPerSample / 8); // byte rate
                writer.Write((short)(channels * bitsPerSample / 8)); // block align
                writer.Write((short)bitsPerSample);

                // data chunk
                writer.Write(new char[] { 'd', 'a', 't', 'a' });
                writer.Write(dataSize);

                // Write a short beep (sine wave at 440Hz for half the duration, then silence)
                int beepSamples = numSamples / 2;
                for (int i = 0; i < numSamples; i++)
                {
                    short sample = 0;
                    if (i < beepSamples)
                    {
                        float t = (float)i / sampleRate;
                        sample = (short)(short.MaxValue * 0.3f * Mathf.Sin(2f * Mathf.PI * 440f * t));
                    }
                    writer.Write(sample);
                }
            }
        }

        // ─── LETTER ASSETS ──────────────────────────────────────────

        private static LetterData[] CreateLetterAssets()
        {
            string path = $"{BasePath}/Data/Letters";
            EnsureDirectory(path);

            var letters = new LetterData[Alphabet.Length];

            for (int i = 0; i < Alphabet.Length; i++)
            {
                var (arabic, name, translit) = Alphabet[i];
                string assetPath = $"{path}/{name}.asset";

                var existing = AssetDatabase.LoadAssetAtPath<LetterData>(assetPath);
                if (existing != null)
                {
                    letters[i] = existing;
                    continue;
                }

                var letter = ScriptableObject.CreateInstance<LetterData>();
                letter.letterArabic = arabic;
                letter.letterName = name;
                letter.transliteration = translit;
                letter.orderIndex = i;

                // Link placeholder audio
                string audioPath = $"{BasePath}/Audio/Letters/{name.ToLower()}.wav";
                letter.audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);

                AssetDatabase.CreateAsset(letter, assetPath);
                letters[i] = letter;
            }

            return letters;
        }

        // ─── GROUP ASSETS ───────────────────────────────────────────

        private static LetterGroupData[] CreateGroupAssets(LetterData[] letters)
        {
            string path = $"{BasePath}/Data/Groups";
            EnsureDirectory(path);

            var groups = new LetterGroupData[Groups.Length];

            for (int g = 0; g < Groups.Length; g++)
            {
                var (name, start, count) = Groups[g];
                string assetPath = $"{path}/Group{g + 1}.asset";

                var existing = AssetDatabase.LoadAssetAtPath<LetterGroupData>(assetPath);
                if (existing != null)
                {
                    groups[g] = existing;
                    continue;
                }

                var group = ScriptableObject.CreateInstance<LetterGroupData>();
                group.groupName = name;
                group.groupIndex = g;
                group.questionsPerQuiz = 10;

                group.letters = new LetterData[count];
                for (int i = 0; i < count; i++)
                    group.letters[i] = letters[start + i];

                AssetDatabase.CreateAsset(group, assetPath);
                groups[g] = group;
            }

            return groups;
        }

        // ─── LESSON ASSET ───────────────────────────────────────────

        private static void CreateLessonAsset(LetterData[] letters, LetterGroupData[] groups)
        {
            string path = $"{BasePath}/Data/Lessons";
            EnsureDirectory(path);

            string assetPath = $"{path}/Lesson1.asset";
            if (AssetDatabase.LoadAssetAtPath<LessonData>(assetPath) != null) return;

            var lesson = ScriptableObject.CreateInstance<LessonData>();
            lesson.lessonId = "lesson_1";
            lesson.lessonTitle = "The Arabic Alphabet";
            lesson.lessonNumber = 1;
            lesson.groups = groups;
            lesson.allLetters = letters;

            AssetDatabase.CreateAsset(lesson, assetPath);
        }

        // ─── SCENE CREATION ─────────────────────────────────────────

        private static void CreateBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGO = new GameObject("Main Camera");
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.backgroundColor = new Color(1f, 0.97f, 0.94f); // #FFF8F0
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(0, 0, -10);

            // GameManager
            var gmGO = new GameObject("GameManager");
            var gm = gmGO.AddComponent<GameManager>();
            // We'll need to assign lessons after scene load in editor

            // AudioManager
            var amGO = new GameObject("AudioManager");
            amGO.AddComponent<AudioManager>();
            var letterSource = amGO.AddComponent<AudioSource>();
            letterSource.playOnAwake = false;
            var sfxSource = amGO.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            // SaveManager
            var smGO = new GameObject("SaveManager");
            smGO.AddComponent<SaveManager>();

            // BootLoader
            var bootGO = new GameObject("BootLoader");
            bootGO.AddComponent<BootLoader>();

            // Splash Canvas
            var canvasGO = CreateCanvas("SplashCanvas");
            var panel = CreatePanel(canvasGO.transform, "SplashPanel", new Color(1f, 0.97f, 0.94f));
            var titleText = CreateText(panel.transform, "TitleText", "SimpleQaidah", 72, TextAlignmentOptions.Center);
            var subtitleText = CreateText(panel.transform, "SubtitleText", "Al-Qa'idah Al-Nooraniyyah", 32, TextAlignmentOptions.Center);
            subtitleText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -80);

            string scenePath = $"{BasePath}/Scenes/BootScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGO = new GameObject("Main Camera");
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.backgroundColor = new Color(1f, 0.97f, 0.94f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(0, 0, -10);

            // EventSystem
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // Canvas
            var canvasGO = CreateCanvas("MainCanvas");

            // Background
            CreatePanel(canvasGO.transform, "Background", new Color(1f, 0.97f, 0.94f));

            // Header
            var header = CreatePanel(canvasGO.transform, "Header", new Color(0.08f, 0.40f, 0.75f)); // #1565C0
            var headerRT = header.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0, 0.85f);
            headerRT.anchorMax = new Vector2(1, 1);
            headerRT.offsetMin = Vector2.zero;
            headerRT.offsetMax = Vector2.zero;

            var titleText = CreateText(header.transform, "TitleText", "SimpleQaidah", 48, TextAlignmentOptions.Center);
            titleText.color = Color.white;
            var titleRT = titleText.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 0.4f);
            titleRT.anchorMax = new Vector2(1, 0.9f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;

            var xpText = CreateText(header.transform, "TotalXPText", "0 XP", 24, TextAlignmentOptions.Center);
            xpText.color = new Color(1f, 0.84f, 0f); // gold
            var xpRT = xpText.GetComponent<RectTransform>();
            xpRT.anchorMin = new Vector2(0, 0);
            xpRT.anchorMax = new Vector2(1, 0.4f);
            xpRT.offsetMin = Vector2.zero;
            xpRT.offsetMax = Vector2.zero;

            // Lesson 1 Button Area
            var lessonArea = CreatePanel(canvasGO.transform, "LessonArea", new Color(1f, 0.97f, 0.94f, 0f));
            var laRT = lessonArea.GetComponent<RectTransform>();
            laRT.anchorMin = new Vector2(0.05f, 0.55f);
            laRT.anchorMax = new Vector2(0.95f, 0.82f);
            laRT.offsetMin = Vector2.zero;
            laRT.offsetMax = Vector2.zero;

            // Lesson 1 Button
            var lesson1BtnGO = new GameObject("Lesson1Button");
            lesson1BtnGO.transform.SetParent(lessonArea.transform, false);
            var l1Img = lesson1BtnGO.AddComponent<Image>();
            l1Img.color = new Color(0.31f, 0.76f, 0.97f); // #4FC3F7
            var l1Btn = lesson1BtnGO.AddComponent<Button>();
            l1Btn.targetGraphic = l1Img;
            var l1RT = lesson1BtnGO.GetComponent<RectTransform>();
            l1RT.anchorMin = Vector2.zero;
            l1RT.anchorMax = Vector2.one;
            l1RT.offsetMin = Vector2.zero;
            l1RT.offsetMax = Vector2.zero;

            var l1Title = CreateText(lesson1BtnGO.transform, "Lesson1TitleText", "Lesson 1: The Arabic Alphabet", 32, TextAlignmentOptions.Center);
            l1Title.color = Color.white;
            var l1TitleRT = l1Title.GetComponent<RectTransform>();
            l1TitleRT.anchorMin = new Vector2(0.05f, 0.5f);
            l1TitleRT.anchorMax = new Vector2(0.95f, 0.9f);
            l1TitleRT.offsetMin = Vector2.zero;
            l1TitleRT.offsetMax = Vector2.zero;

            var l1Progress = CreateText(lesson1BtnGO.transform, "Lesson1ProgressText", "0/6 groups  |  0 stars", 22, TextAlignmentOptions.Center);
            l1Progress.color = new Color(1, 1, 1, 0.8f);
            var l1ProgRT = l1Progress.GetComponent<RectTransform>();
            l1ProgRT.anchorMin = new Vector2(0.05f, 0.15f);
            l1ProgRT.anchorMax = new Vector2(0.95f, 0.5f);
            l1ProgRT.offsetMin = Vector2.zero;
            l1ProgRT.offsetMax = Vector2.zero;

            // Progress fill bar
            var progBarBG = new GameObject("ProgressBarBG");
            progBarBG.transform.SetParent(lesson1BtnGO.transform, false);
            var pbBGImg = progBarBG.AddComponent<Image>();
            pbBGImg.color = new Color(0, 0, 0, 0.2f);
            var pbBGRT = progBarBG.GetComponent<RectTransform>();
            pbBGRT.anchorMin = new Vector2(0.1f, 0.05f);
            pbBGRT.anchorMax = new Vector2(0.9f, 0.12f);
            pbBGRT.offsetMin = Vector2.zero;
            pbBGRT.offsetMax = Vector2.zero;

            var progBarFill = new GameObject("ProgressBarFill");
            progBarFill.transform.SetParent(progBarBG.transform, false);
            var pbFillImg = progBarFill.AddComponent<Image>();
            pbFillImg.color = new Color(1f, 0.84f, 0f); // gold
            pbFillImg.type = Image.Type.Filled;
            pbFillImg.fillMethod = Image.FillMethod.Horizontal;
            pbFillImg.fillAmount = 0f;
            var pbFillRT = progBarFill.GetComponent<RectTransform>();
            pbFillRT.anchorMin = Vector2.zero;
            pbFillRT.anchorMax = Vector2.one;
            pbFillRT.offsetMin = Vector2.zero;
            pbFillRT.offsetMax = Vector2.zero;

            // MainMenuUI script
            var menuUI = canvasGO.AddComponent<MainMenuUI>();
            // References will need to be wired in the editor after first run
            // We'll use SerializeField, so they need to be assigned in Inspector

            // UIStyler for visual polish
            canvasGO.AddComponent<UIStyler>();

            string scenePath = $"{BasePath}/Scenes/MainMenuScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void CreateLessonScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGO = new GameObject("Main Camera");
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.backgroundColor = new Color(1f, 0.97f, 0.94f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(0, 0, -10);

            // EventSystem
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // Canvas
            var canvasGO = CreateCanvas("LessonCanvas");

            // ─── Header Bar (persistent) ─────────────────────────
            var headerBar = CreatePanel(canvasGO.transform, "HeaderBar", new Color(0.08f, 0.40f, 0.75f));
            var hbRT = headerBar.GetComponent<RectTransform>();
            hbRT.anchorMin = new Vector2(0, 0.92f);
            hbRT.anchorMax = new Vector2(1, 1);
            hbRT.offsetMin = Vector2.zero;
            hbRT.offsetMax = Vector2.zero;

            // Back button
            var backBtnGO = new GameObject("BackButton");
            backBtnGO.transform.SetParent(headerBar.transform, false);
            var backImg = backBtnGO.AddComponent<Image>();
            backImg.color = new Color(1, 1, 1, 0.3f);
            backBtnGO.AddComponent<Button>().targetGraphic = backImg;
            var backRT = backBtnGO.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0, 0);
            backRT.anchorMax = new Vector2(0.15f, 1);
            backRT.offsetMin = Vector2.zero;
            backRT.offsetMax = Vector2.zero;
            var backText = CreateText(backBtnGO.transform, "BackText", "<", 36, TextAlignmentOptions.Center);
            backText.color = Color.white;
            SetRectFull(backText.GetComponent<RectTransform>());

            // Title
            var lessonTitle = CreateText(headerBar.transform, "LessonTitle", "Lesson 1: The Alphabet", 28, TextAlignmentOptions.Center);
            lessonTitle.color = Color.white;
            var ltRT = lessonTitle.GetComponent<RectTransform>();
            ltRT.anchorMin = new Vector2(0.15f, 0);
            ltRT.anchorMax = new Vector2(0.85f, 1);
            ltRT.offsetMin = Vector2.zero;
            ltRT.offsetMax = Vector2.zero;

            // ─── Alphabet Grid Panel ─────────────────────────────
            var gridPanel = CreatePanel(canvasGO.transform, "AlphabetGridPanel", new Color(1, 0.97f, 0.94f, 0f));
            var gpRT = gridPanel.GetComponent<RectTransform>();
            gpRT.anchorMin = new Vector2(0, 0);
            gpRT.anchorMax = new Vector2(1, 0.92f);
            gpRT.offsetMin = Vector2.zero;
            gpRT.offsetMax = Vector2.zero;

            // Grid Container with GridLayoutGroup
            var gridContainer = new GameObject("GridContainer");
            gridContainer.transform.SetParent(gridPanel.transform, false);
            var gcRT = EnsureRectTransform(gridContainer);
            gcRT.anchorMin = new Vector2(0.03f, 0.15f);
            gcRT.anchorMax = new Vector2(0.97f, 0.95f);
            gcRT.offsetMin = Vector2.zero;
            gcRT.offsetMax = Vector2.zero;
            var grid = gridContainer.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 7;
            grid.cellSize = new Vector2(130, 130);
            grid.spacing = new Vector2(10, 10);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(5, 5, 5, 5);

            // Start Button
            var startBtnGO = new GameObject("StartButton");
            startBtnGO.transform.SetParent(gridPanel.transform, false);
            var startImg = startBtnGO.AddComponent<Image>();
            startImg.color = new Color(1f, 0.65f, 0.15f); // Orange #FFA726
            var startBtn = startBtnGO.AddComponent<Button>();
            startBtn.targetGraphic = startImg;
            var sbRT = startBtnGO.GetComponent<RectTransform>();
            sbRT.anchorMin = new Vector2(0.15f, 0.02f);
            sbRT.anchorMax = new Vector2(0.85f, 0.12f);
            sbRT.offsetMin = Vector2.zero;
            sbRT.offsetMax = Vector2.zero;
            var startText = CreateText(startBtnGO.transform, "StartText", "Start Learning", 32, TextAlignmentOptions.Center);
            startText.color = Color.white;
            SetRectFull(startText.GetComponent<RectTransform>());

            var alphabetGridUI = gridPanel.AddComponent<AlphabetGridUI>();

            // ─── Learn Panel ─────────────────────────────────────
            var learnPanel = CreatePanel(canvasGO.transform, "LearnPanel", new Color(1, 0.97f, 0.94f));
            var lpRT = learnPanel.GetComponent<RectTransform>();
            lpRT.anchorMin = new Vector2(0, 0);
            lpRT.anchorMax = new Vector2(1, 0.92f);
            lpRT.offsetMin = Vector2.zero;
            lpRT.offsetMax = Vector2.zero;
            learnPanel.SetActive(false);

            // Large letter
            var largeLetter = CreateText(learnPanel.transform, "LargeLetterText", "ا", 200, TextAlignmentOptions.Center);
            largeLetter.isRightToLeftText = true;
            var llRT = largeLetter.GetComponent<RectTransform>();
            llRT.anchorMin = new Vector2(0.1f, 0.45f);
            llRT.anchorMax = new Vector2(0.9f, 0.85f);
            llRT.offsetMin = Vector2.zero;
            llRT.offsetMax = Vector2.zero;

            // Letter name
            var letterName = CreateText(learnPanel.transform, "LetterNameText", "Alif", 48, TextAlignmentOptions.Center);
            var lnRT = letterName.GetComponent<RectTransform>();
            lnRT.anchorMin = new Vector2(0.1f, 0.35f);
            lnRT.anchorMax = new Vector2(0.9f, 0.45f);
            lnRT.offsetMin = Vector2.zero;
            lnRT.offsetMax = Vector2.zero;

            // Transliteration
            var translit = CreateText(learnPanel.transform, "TransliterationText", "a", 32, TextAlignmentOptions.Center);
            translit.color = new Color(0.5f, 0.5f, 0.5f);
            var trRT = translit.GetComponent<RectTransform>();
            trRT.anchorMin = new Vector2(0.1f, 0.28f);
            trRT.anchorMax = new Vector2(0.9f, 0.35f);
            trRT.offsetMin = Vector2.zero;
            trRT.offsetMax = Vector2.zero;

            // Speaker button
            var speakerBtnGO = new GameObject("SpeakerButton");
            speakerBtnGO.transform.SetParent(learnPanel.transform, false);
            var spkImg = speakerBtnGO.AddComponent<Image>();
            spkImg.color = new Color(0.31f, 0.76f, 0.97f);
            speakerBtnGO.AddComponent<Button>().targetGraphic = spkImg;
            var spkRT = speakerBtnGO.GetComponent<RectTransform>();
            spkRT.anchorMin = new Vector2(0.4f, 0.18f);
            spkRT.anchorMax = new Vector2(0.6f, 0.26f);
            spkRT.offsetMin = Vector2.zero;
            spkRT.offsetMax = Vector2.zero;
            var spkText = CreateText(speakerBtnGO.transform, "SpeakerText", "🔊", 32, TextAlignmentOptions.Center);
            SetRectFull(spkText.GetComponent<RectTransform>());

            // Navigation dots container
            var dotsContainer = new GameObject("DotsContainer");
            dotsContainer.transform.SetParent(learnPanel.transform, false);
            var dcRT = EnsureRectTransform(dotsContainer);
            dcRT.anchorMin = new Vector2(0.3f, 0.12f);
            dcRT.anchorMax = new Vector2(0.7f, 0.17f);
            dcRT.offsetMin = Vector2.zero;
            dcRT.offsetMax = Vector2.zero;
            var dotsLayout = dotsContainer.AddComponent<HorizontalLayoutGroup>();
            dotsLayout.spacing = 15;
            dotsLayout.childAlignment = TextAnchor.MiddleCenter;
            dotsLayout.childForceExpandWidth = false;
            dotsLayout.childForceExpandHeight = false;

            // Counter text
            var counterText = CreateText(learnPanel.transform, "CounterText", "1 / 5", 24, TextAlignmentOptions.Center);
            counterText.color = new Color(0.5f, 0.5f, 0.5f);
            var ctRT = counterText.GetComponent<RectTransform>();
            ctRT.anchorMin = new Vector2(0.3f, 0.06f);
            ctRT.anchorMax = new Vector2(0.7f, 0.12f);
            ctRT.offsetMin = Vector2.zero;
            ctRT.offsetMax = Vector2.zero;

            // Prev button
            var prevBtnGO = CreateNavButton(learnPanel.transform, "PrevButton", "<", new Vector2(0.05f, 0.5f), new Vector2(0.15f, 0.7f));

            // Next button
            var nextBtnGO = CreateNavButton(learnPanel.transform, "NextButton", ">", new Vector2(0.85f, 0.5f), new Vector2(0.95f, 0.7f));

            // Practice button
            var practiceBtnGO = new GameObject("PracticeButton");
            practiceBtnGO.transform.SetParent(learnPanel.transform, false);
            var pracImg = practiceBtnGO.AddComponent<Image>();
            pracImg.color = new Color(0.40f, 0.73f, 0.42f); // Green
            practiceBtnGO.AddComponent<Button>().targetGraphic = pracImg;
            var pracRT = practiceBtnGO.GetComponent<RectTransform>();
            pracRT.anchorMin = new Vector2(0.15f, 0.02f);
            pracRT.anchorMax = new Vector2(0.85f, 0.1f);
            pracRT.offsetMin = Vector2.zero;
            pracRT.offsetMax = Vector2.zero;
            var pracText = CreateText(practiceBtnGO.transform, "PracticeText", "Start Practice", 32, TextAlignmentOptions.Center);
            pracText.color = Color.white;
            SetRectFull(pracText.GetComponent<RectTransform>());
            practiceBtnGO.SetActive(false);

            var learnPhaseUI = learnPanel.AddComponent<LearnPhaseUI>();

            // ─── Quiz Panel ──────────────────────────────────────
            var quizPanel = CreatePanel(canvasGO.transform, "QuizPanel", new Color(1, 0.97f, 0.94f));
            var qpRT = quizPanel.GetComponent<RectTransform>();
            qpRT.anchorMin = new Vector2(0, 0);
            qpRT.anchorMax = new Vector2(1, 0.92f);
            qpRT.offsetMin = Vector2.zero;
            qpRT.offsetMax = Vector2.zero;
            quizPanel.SetActive(false);

            // Quiz progress bar
            var quizProgBG = new GameObject("QuizProgressBG");
            quizProgBG.transform.SetParent(quizPanel.transform, false);
            var qpbImg = quizProgBG.AddComponent<Image>();
            qpbImg.color = new Color(0.9f, 0.9f, 0.9f);
            var qpbRT = quizProgBG.GetComponent<RectTransform>();
            qpbRT.anchorMin = new Vector2(0.05f, 0.92f);
            qpbRT.anchorMax = new Vector2(0.95f, 0.95f);
            qpbRT.offsetMin = Vector2.zero;
            qpbRT.offsetMax = Vector2.zero;

            var quizProgFill = new GameObject("QuizProgressFill");
            quizProgFill.transform.SetParent(quizProgBG.transform, false);
            var qpfImg = quizProgFill.AddComponent<Image>();
            qpfImg.color = new Color(1f, 0.65f, 0.15f); // Orange
            qpfImg.type = Image.Type.Filled;
            qpfImg.fillMethod = Image.FillMethod.Horizontal;
            qpfImg.fillAmount = 0f;
            SetRectFull(quizProgFill.GetComponent<RectTransform>());

            var quizProgText = CreateText(quizPanel.transform, "QuizProgressText", "1 / 10", 22, TextAlignmentOptions.Center);
            var qptRT = quizProgText.GetComponent<RectTransform>();
            qptRT.anchorMin = new Vector2(0.3f, 0.86f);
            qptRT.anchorMax = new Vector2(0.7f, 0.92f);
            qptRT.offsetMin = Vector2.zero;
            qptRT.offsetMax = Vector2.zero;

            // Question area
            var questionText = CreateText(quizPanel.transform, "QuestionText", "Which letter is this?", 32, TextAlignmentOptions.Center);
            var qqRT = questionText.GetComponent<RectTransform>();
            qqRT.anchorMin = new Vector2(0.05f, 0.75f);
            qqRT.anchorMax = new Vector2(0.95f, 0.85f);
            qqRT.offsetMin = Vector2.zero;
            qqRT.offsetMax = Vector2.zero;

            // Prompt letter (for LetterToName quiz type)
            var promptLetter = CreateText(quizPanel.transform, "PromptLetterText", "ب", 120, TextAlignmentOptions.Center);
            promptLetter.isRightToLeftText = true;
            var plRT = promptLetter.GetComponent<RectTransform>();
            plRT.anchorMin = new Vector2(0.2f, 0.5f);
            plRT.anchorMax = new Vector2(0.8f, 0.75f);
            plRT.offsetMin = Vector2.zero;
            plRT.offsetMax = Vector2.zero;

            // Prompt name (for NameToLetter quiz type)
            var promptName = CreateText(quizPanel.transform, "PromptNameText", "Ba", 48, TextAlignmentOptions.Center);
            var pnRT = promptName.GetComponent<RectTransform>();
            pnRT.anchorMin = new Vector2(0.2f, 0.55f);
            pnRT.anchorMax = new Vector2(0.8f, 0.7f);
            pnRT.offsetMin = Vector2.zero;
            pnRT.offsetMax = Vector2.zero;

            // Speaker button for audio quiz
            var quizSpeakerGO = new GameObject("QuizSpeakerButton");
            quizSpeakerGO.transform.SetParent(quizPanel.transform, false);
            var qsImg = quizSpeakerGO.AddComponent<Image>();
            qsImg.color = new Color(0.31f, 0.76f, 0.97f);
            quizSpeakerGO.AddComponent<Button>().targetGraphic = qsImg;
            var qsRT = quizSpeakerGO.GetComponent<RectTransform>();
            qsRT.anchorMin = new Vector2(0.35f, 0.55f);
            qsRT.anchorMax = new Vector2(0.65f, 0.7f);
            qsRT.offsetMin = Vector2.zero;
            qsRT.offsetMax = Vector2.zero;
            var qsText = CreateText(quizSpeakerGO.transform, "SpeakerIcon", "🔊 Play", 28, TextAlignmentOptions.Center);
            qsText.color = Color.white;
            SetRectFull(qsText.GetComponent<RectTransform>());

            // Options grid (2x2)
            var optionsGrid = new GameObject("OptionsGrid");
            optionsGrid.transform.SetParent(quizPanel.transform, false);
            var ogRT = EnsureRectTransform(optionsGrid);
            ogRT.anchorMin = new Vector2(0.05f, 0.05f);
            ogRT.anchorMax = new Vector2(0.95f, 0.5f);
            ogRT.offsetMin = Vector2.zero;
            ogRT.offsetMax = Vector2.zero;
            var ogGrid = optionsGrid.AddComponent<GridLayoutGroup>();
            ogGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            ogGrid.constraintCount = 2;
            ogGrid.cellSize = new Vector2(480, 180);
            ogGrid.spacing = new Vector2(20, 20);
            ogGrid.childAlignment = TextAnchor.MiddleCenter;
            ogGrid.padding = new RectOffset(10, 10, 10, 10);

            // Create 4 option buttons
            var optionButtons = new Button[4];
            var optionTexts = new TMP_Text[4];
            var optionBGs = new Image[4];
            Color[] optColors = {
                new Color(0.31f, 0.76f, 0.97f), // Blue
                new Color(0.67f, 0.28f, 0.74f), // Purple
                new Color(1f, 0.65f, 0.15f),     // Orange
                new Color(0.93f, 0.25f, 0.48f),  // Pink
            };

            for (int i = 0; i < 4; i++)
            {
                var optGO = new GameObject($"Option{i + 1}");
                optGO.transform.SetParent(optionsGrid.transform, false);
                var optImg = optGO.AddComponent<Image>();
                optImg.color = optColors[i];
                var optBtn = optGO.AddComponent<Button>();
                optBtn.targetGraphic = optImg;
                var optText = CreateText(optGO.transform, "OptionText", $"Option {i + 1}", 36, TextAlignmentOptions.Center);
                optText.color = Color.white;
                SetRectFull(optText.GetComponent<RectTransform>());

                optionButtons[i] = optBtn;
                optionTexts[i] = optText;
                optionBGs[i] = optImg;
            }

            var quizPhaseUI = quizPanel.AddComponent<QuizPhaseUI>();

            // ─── Result Panel ────────────────────────────────────
            var resultPanel = CreatePanel(canvasGO.transform, "ResultPanel", new Color(1, 0.97f, 0.94f));
            var rpRT = resultPanel.GetComponent<RectTransform>();
            rpRT.anchorMin = new Vector2(0, 0);
            rpRT.anchorMax = new Vector2(1, 0.92f);
            rpRT.offsetMin = Vector2.zero;
            rpRT.offsetMax = Vector2.zero;
            resultPanel.SetActive(false);

            // Message text
            var messageText = CreateText(resultPanel.transform, "MessageText", "Great job!", 48, TextAlignmentOptions.Center);
            messageText.color = new Color(0.08f, 0.40f, 0.75f);
            var msgRT = messageText.GetComponent<RectTransform>();
            msgRT.anchorMin = new Vector2(0.1f, 0.75f);
            msgRT.anchorMax = new Vector2(0.9f, 0.9f);
            msgRT.offsetMin = Vector2.zero;
            msgRT.offsetMax = Vector2.zero;

            // Stars area (3 star images)
            var starsArea = new GameObject("StarsArea");
            starsArea.transform.SetParent(resultPanel.transform, false);
            var saRT = EnsureRectTransform(starsArea);
            saRT.anchorMin = new Vector2(0.15f, 0.55f);
            saRT.anchorMax = new Vector2(0.85f, 0.75f);
            saRT.offsetMin = Vector2.zero;
            saRT.offsetMax = Vector2.zero;
            var saLayout = starsArea.AddComponent<HorizontalLayoutGroup>();
            saLayout.spacing = 30;
            saLayout.childAlignment = TextAnchor.MiddleCenter;
            saLayout.childForceExpandWidth = false;
            saLayout.childForceExpandHeight = false;

            var starImages = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var starGO = new GameObject($"Star{i + 1}");
                starGO.transform.SetParent(starsArea.transform, false);
                var starImg = starGO.AddComponent<Image>();
                starImg.color = new Color(0.5f, 0.5f, 0.5f); // grey = empty
                var starLE = starGO.AddComponent<LayoutElement>();
                starLE.preferredWidth = 80;
                starLE.preferredHeight = 80;
                starImages[i] = starImg;

                var starLabel = CreateText(starGO.transform, "StarLabel", "★", 60, TextAlignmentOptions.Center);
                SetRectFull(starLabel.GetComponent<RectTransform>());
            }

            // Score text
            var scoreText = CreateText(resultPanel.transform, "ScoreText", "8 / 10", 36, TextAlignmentOptions.Center);
            var scRT = scoreText.GetComponent<RectTransform>();
            scRT.anchorMin = new Vector2(0.2f, 0.45f);
            scRT.anchorMax = new Vector2(0.8f, 0.55f);
            scRT.offsetMin = Vector2.zero;
            scRT.offsetMax = Vector2.zero;

            // XP gain text
            var xpGain = CreateText(resultPanel.transform, "XPGainText", "+100 XP", 32, TextAlignmentOptions.Center);
            xpGain.color = new Color(1f, 0.84f, 0f); // Gold
            var xgRT = xpGain.GetComponent<RectTransform>();
            xgRT.anchorMin = new Vector2(0.2f, 0.35f);
            xgRT.anchorMax = new Vector2(0.8f, 0.45f);
            xgRT.offsetMin = Vector2.zero;
            xgRT.offsetMax = Vector2.zero;

            // Retry button
            var retryBtnGO = new GameObject("RetryButton");
            retryBtnGO.transform.SetParent(resultPanel.transform, false);
            var retImg = retryBtnGO.AddComponent<Image>();
            retImg.color = new Color(0.94f, 0.33f, 0.31f); // Red
            retryBtnGO.AddComponent<Button>().targetGraphic = retImg;
            var retRT = retryBtnGO.GetComponent<RectTransform>();
            retRT.anchorMin = new Vector2(0.1f, 0.1f);
            retRT.anchorMax = new Vector2(0.48f, 0.22f);
            retRT.offsetMin = Vector2.zero;
            retRT.offsetMax = Vector2.zero;
            var retText = CreateText(retryBtnGO.transform, "RetryText", "Retry", 28, TextAlignmentOptions.Center);
            retText.color = Color.white;
            SetRectFull(retText.GetComponent<RectTransform>());

            // Continue button
            var contBtnGO = new GameObject("ContinueButton");
            contBtnGO.transform.SetParent(resultPanel.transform, false);
            var contImg = contBtnGO.AddComponent<Image>();
            contImg.color = new Color(0.40f, 0.73f, 0.42f); // Green
            contBtnGO.AddComponent<Button>().targetGraphic = contImg;
            var contRT = contBtnGO.GetComponent<RectTransform>();
            contRT.anchorMin = new Vector2(0.52f, 0.1f);
            contRT.anchorMax = new Vector2(0.9f, 0.22f);
            contRT.offsetMin = Vector2.zero;
            contRT.offsetMax = Vector2.zero;
            var contText = CreateText(contBtnGO.transform, "ContinueText", "Continue", 28, TextAlignmentOptions.Center);
            contText.color = Color.white;
            SetRectFull(contText.GetComponent<RectTransform>());

            var quizResultUI = resultPanel.AddComponent<QuizResultUI>();

            // ─── LessonFlowController ────────────────────────────
            var flowControllerGO = new GameObject("LessonFlowController");
            flowControllerGO.transform.SetParent(canvasGO.transform, false);
            var flowController = flowControllerGO.AddComponent<LessonFlowController>();

            // UIStyler for visual polish
            canvasGO.AddComponent<UIStyler>();

            string scenePath = $"{BasePath}/Scenes/LessonScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        // ─── PREFABS ────────────────────────────────────────────

        private static void CreateLetterCardPrefab()
        {
            string prefabPath = $"{BasePath}/Prefabs/LetterCard.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

            var go = new GameObject("LetterCard");

            // Background
            var bgImg = go.AddComponent<Image>();
            bgImg.color = new Color(0.31f, 0.76f, 0.97f); // Default blue

            // Button
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bgImg;

            // Letter text
            var letterTextGO = new GameObject("LetterText");
            letterTextGO.transform.SetParent(go.transform, false);
            var letterText = letterTextGO.AddComponent<TextMeshProUGUI>();
            letterText.text = "ا";
            letterText.fontSize = 52;
            letterText.alignment = TextAlignmentOptions.Center;
            letterText.color = Color.white;
            letterText.isRightToLeftText = true;
            var ltRT = letterTextGO.GetComponent<RectTransform>();
            ltRT.anchorMin = Vector2.zero;
            ltRT.anchorMax = Vector2.one;
            ltRT.offsetMin = Vector2.zero;
            ltRT.offsetMax = Vector2.zero;

            // Lock overlay
            var lockGO = new GameObject("LockOverlay");
            lockGO.transform.SetParent(go.transform, false);
            var lockImg = lockGO.AddComponent<Image>();
            lockImg.color = new Color(0, 0, 0, 0.4f);
            SetRectFull(lockGO.GetComponent<RectTransform>());
            var lockIconGO = new GameObject("LockIcon");
            lockIconGO.transform.SetParent(lockGO.transform, false);
            var lockIconImg = lockIconGO.AddComponent<Image>();
            lockIconImg.color = Color.white;
            lockIconImg.preserveAspect = true;
            // Sprite assigned at runtime by LetterCardUI via SpriteGenerator
            var lockIconRT = lockIconGO.GetComponent<RectTransform>();
            lockIconRT.anchorMin = new Vector2(0.25f, 0.25f);
            lockIconRT.anchorMax = new Vector2(0.75f, 0.75f);
            lockIconRT.offsetMin = Vector2.zero;
            lockIconRT.offsetMax = Vector2.zero;
            lockGO.SetActive(false);

            // Check overlay
            var checkGO = new GameObject("CheckOverlay");
            checkGO.transform.SetParent(go.transform, false);
            var checkRT = EnsureRectTransform(checkGO);
            checkRT.anchorMin = new Vector2(0.55f, 0.55f);
            checkRT.anchorMax = new Vector2(1f, 1f);
            checkRT.offsetMin = Vector2.zero;
            checkRT.offsetMax = Vector2.zero;
            var checkBgImg = checkGO.AddComponent<Image>();
            checkBgImg.color = new Color(0.40f, 0.73f, 0.42f, 0.9f);
            var checkIconGO = new GameObject("CheckIcon");
            checkIconGO.transform.SetParent(checkGO.transform, false);
            var checkIconImg = checkIconGO.AddComponent<Image>();
            checkIconImg.color = Color.white;
            checkIconImg.preserveAspect = true;
            // Sprite assigned at runtime by LetterCardUI via SpriteGenerator
            var checkIconRT = checkIconGO.GetComponent<RectTransform>();
            checkIconRT.anchorMin = new Vector2(0.15f, 0.15f);
            checkIconRT.anchorMax = new Vector2(0.85f, 0.85f);
            checkIconRT.offsetMin = Vector2.zero;
            checkIconRT.offsetMax = Vector2.zero;
            checkGO.SetActive(false);

            // LetterCardUI component
            go.AddComponent<LetterCardUI>();

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        private static void CreateDotPrefab()
        {
            string prefabPath = $"{BasePath}/Prefabs/Dot.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

            var go = new GameObject("Dot");
            var img = go.AddComponent<Image>();
            img.color = new Color(0.74f, 0.74f, 0.74f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 15;
            le.preferredHeight = 15;

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        // ─── BUILD SETTINGS ─────────────────────────────────────

        private static void AddScenesToBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene($"{BasePath}/Scenes/BootScene.unity", true),
                new EditorBuildSettingsScene($"{BasePath}/Scenes/MainMenuScene.unity", true),
                new EditorBuildSettingsScene($"{BasePath}/Scenes/LessonScene.unity", true),
            };
            EditorBuildSettings.scenes = scenes;
        }

        // ─── HELPERS ────────────────────────────────────────────

        private static RectTransform EnsureRectTransform(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            return rt;
        }

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folderName = Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent))
                    EnsureDirectory(parent);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static GameObject CreateCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.black;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return tmp;
        }

        private static void SetRectFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject CreateNavButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.31f, 0.76f, 0.97f);
            go.AddComponent<Button>().targetGraphic = img;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var text = CreateText(go.transform, "Label", label, 36, TextAlignmentOptions.Center);
            text.color = Color.white;
            SetRectFull(text.GetComponent<RectTransform>());
            return go;
        }
    }
}
#endif
