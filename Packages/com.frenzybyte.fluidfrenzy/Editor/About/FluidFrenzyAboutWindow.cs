using UnityEngine;
using UnityEditor;
using System.Text;
using System.Security.Cryptography;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using System.IO;


namespace FluidFrenzy.Editor
{
    // Open on first load.
    [InitializeOnLoad]
    public class FluidFrenzyAboutWindow : EditorWindow
    {
        private const string kWindowOpenedKey = "FluidFrenzyAboutOpened";
        private const string kPackageName = "com.frenzybyte.fluidfrenzy";
        private const string kDocumentationURL = "https://frenzybyte.github.io/fluidfrenzy/docs/index/";
        private const string kTutorialsURL = "https://www.youtube.com/watch?v=wqjPYXO3rqI&list=PLNdmEnvAr4DR66Yea3XBGuRuGFIijWMAS";
        private const string kChangelogURL = "https://frenzybyte.github.io/fluidfrenzy/changelog/index/";
        private const string kDiscordURL = "https://discord.gg/26QcnZ6Q9k";
        private const string kRequestURL = "https://github.com/FrenzyByte/fluidfrenzy/issues";

		class Styles
		{
			public static GUIContent documentationLabel = new GUIContent("Documentation", kDocumentationURL);
			public static GUIContent tutorialsLabel = new GUIContent("Tutorials", kTutorialsURL);
			public static GUIContent changelogLabel = new GUIContent("Changelog", kChangelogURL);
			public static GUIContent discordLabel = new GUIContent("Discord", kDiscordURL);
			public static GUIContent requestLabel = new GUIContent("Report Issue", kRequestURL);
		}

        private PackageInfo m_fluidFrendyPackage;
        private Texture m_banner;

        private static string GetMd5Hash(string input)
        {
            MD5 md5 = MD5.Create();
            byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sb = new StringBuilder(); for (int i = 0; i < data.Length; i++)
                sb.Append(data[i].ToString("x2")); return sb.ToString();
        }

        static FluidFrenzyAboutWindow()
        {
            // Open the Editor Window.
            EditorApplication.update += Startup;
        }

        static void Startup()
        {
            EditorApplication.update -= Startup;
            string projectKey = GetMd5Hash(Application.dataPath) + kWindowOpenedKey;
            // Check if the window has been opened before
            if (!EditorPrefs.GetBool(projectKey, false))
            {
                // Set the flag to true so it won't open again.
                EditorPrefs.SetBool(projectKey, true);
                ShowWindow();
            }
        }

        [MenuItem("Window/Fluid Frenzy/About")]
        public static void ShowWindow()
        {
            // Create and show the editor window.
            FluidFrenzyAboutWindow window = GetWindow<FluidFrenzyAboutWindow>("About Fluid Frenzy");
            window.maxSize = new Vector2(600, 375);
            window.minSize = window.maxSize;
            window.Show();
        }

        private void Awake()
        {
#if UNITY_2021_1_OR_NEWER
			// Find the Fluid Frenzy package.
			foreach (PackageInfo info in PackageInfo.GetAllRegisteredPackages())
            {
                if (info.name == kPackageName)
                {
                    m_fluidFrendyPackage = info;
                }
            }
			// Load banner texture.
			m_banner = AssetDatabase.LoadAssetAtPath<Texture>(Path.Join(m_fluidFrendyPackage.assetPath, "Editor/About/FluidFrenzyBanner.png"));
#else
			m_fluidFrendyPackage = PackageInfo.FindForAssetPath("Packages/com.frenzybyte.fluidfrenzy");
			m_banner = AssetDatabase.LoadAssetAtPath<Texture>(m_fluidFrendyPackage.assetPath + "/Editor/About/FluidFrenzyBanner.png");
#endif


		}

#if UNITY_2021_1_OR_NEWER
		private void OnEnable()
		{
			EditorGUI.hyperLinkClicked += OnHyperLinkClicked;
		}

		private void OnDisable()
		{
			EditorGUI.hyperLinkClicked -= OnHyperLinkClicked;
		}
#endif

		private void OnGUI()
        {
			// Window with - 4 pixels on each side.
			float windowWidth = position.width - 8;
			if (m_banner)
			{
				GUILayoutOption[] options = {
					GUILayout.Width(windowWidth),
					GUILayout.Height(m_banner.height * (position.width / m_banner.width)) // Maintain aspect ratio.
				};

				// Draw the banner.
				GUILayout.Label(m_banner, options);
			}
			// Draw top welcome label.
			GUILayout.Label("Welcome and thank you for choosing Fluid Frenzy!", EditorStyles.boldLabel);
            GUIStyle richLabel = new GUIStyle(EditorStyles.label);
            richLabel.richText = true;
            richLabel.wordWrap = true;
            richLabel.focused = EditorStyles.label.normal;

            // Draw rest of the about label with information about the docs.
            EditorGUILayout.SelectableLabel($@"To help you get started, several <b>samples</b> are included in the package. You can easily import these samples using the <b>Package Manager</b> or by clicking the <b>Import Samples</b> button below.

For more in-depth information about using <b>Fluid Frenzy</b> and exploring its features, please refer to the <a url=""{kDocumentationURL}""><b>documentation</b></a> and <a url=""{kTutorialsURL}""><b>tutorial videos</b></a>. Should you encounter any issues while using <b>Fluid Frenzy</b>, please don’t hesitate to reach out <a url=""{kRequestURL}""><b>report them</b></a>. We also invite you to join our <a url=""{kDiscordURL}""><b>Discord channel</b></a> to share your work, ask questions, request features, or discuss all things Fluid Frenzy!

Happy creating, and once again, welcome aboard!"
                , richLabel, GUILayout.ExpandHeight(true), GUILayout.MaxHeight(300));

            //Bottom button bar.
            GUILayout.FlexibleSpace();
            using (var scope = new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Import samples", GUILayout.Width(windowWidth / 2)))
                {
                    ImportSamples();
                }
                if (GUILayout.Button("Open Package Manager"))
                {
                    UnityEditor.PackageManager.UI.Window.Open("Fluid Frenzy");
                }
            }

            //Bottom link bar.
            using (var scope = new GUILayout.HorizontalScope())
            {
#if UNITY_2021_1_OR_NEWER
                if (EditorGUILayout.LinkButton(Styles.documentationLabel))
                {
                    Application.OpenURL(kDocumentationURL);
                }
                if (EditorGUILayout.LinkButton(Styles.tutorialsLabel))
                {
                    Application.OpenURL(kTutorialsURL);
                }
                if (EditorGUILayout.LinkButton(Styles.changelogLabel))
                {
                    Application.OpenURL(kChangelogURL);
                }
                if (EditorGUILayout.LinkButton(Styles.discordLabel))
                {
                    Application.OpenURL(kDiscordURL);
                }
                if (EditorGUILayout.LinkButton(Styles.requestLabel))
                {
                    Application.OpenURL(kRequestURL);
                }
#endif
                //Version label.
                GUILayout.FlexibleSpace();
                using (var versionScape = new GUILayout.VerticalScope())
                {
                    GUILayout.Label("Version: " + m_fluidFrendyPackage.version);
                }
            }
        }

        private void ImportSamples()
        {
            //Go through all samples and import them.
            foreach (var sample in UnityEditor.PackageManager.UI.Sample.FindByPackage(m_fluidFrendyPackage.name, m_fluidFrendyPackage.version))
            {
                sample.Import();
            }
        }

#if UNITY_2021_1_OR_NEWER
		private static void OnHyperLinkClicked(EditorWindow window, HyperLinkClickedEventArgs args)
		{
			if (window is FluidFrenzyAboutWindow)
			{
				var hyperLinkData = args.hyperLinkData;
				var url = hyperLinkData["url"];
				OpenURLWithWarning(url);
			}
		}
#endif

		private static void OpenURLWithWarning(string url)
		{
			if (EditorUtility.DisplayDialog("Opening external website.", $"Fluid Frenzy is trying to open the following URL {url}.\n\nDo you want to open this URL in your browser?", "Yes", "No"))
			{
				Application.OpenURL(url);
			}
		}
	}
}