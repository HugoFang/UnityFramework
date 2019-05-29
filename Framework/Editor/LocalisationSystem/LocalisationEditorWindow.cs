﻿using UnityEditor;
using UnityEngine;

using System;
using System.Collections.Generic;

namespace Framework
{
	using Serialization;
	using Utils;
	using Utils.Editor;

	namespace LocalisationSystem
	{
		namespace Editor
		{
			public sealed class LocalisationEditorWindow : EditorWindow
			{
				public static readonly int kDefaultFontSize = 12;
				public static readonly float kDefaultKeysWidth = 380.0f;
				public static readonly float kDefaultFirstLangagueWidth = 580.0f;

				private static readonly string kWindowWindowName = "Localisation";
				private static readonly string kWindowTitle = "Localisation Editor";
				private static readonly string kEditorPrefKey = "LocalisationEditor.Settings";
				private static readonly string kEditKeyId = "Localisation.EditKey";

				private static readonly float kMinKeysWidth = 180.0f;
				private static readonly float kToolBarHeight = EditorGUIUtility.singleLineHeight * 3;
				private static readonly float kBottomBarHeight = EditorGUIUtility.singleLineHeight;
				private static readonly float kEditKeyBarHeight = EditorGUIUtility.singleLineHeight;

				private static readonly Color kSelectedTextLineBackgroundColor = new Color(1.0f, 0.8f, 0.1f, 1.0f);
				private static readonly Color kSelectedButtonsBackgroundColor = new Color(1.0f, 0.8f, 0.1f, 0.75f);
				private static readonly Color kTextLineBackgroundColorA = new Color(0.7f, 0.7f, 0.7f, 1.0f);
				private static readonly Color kTextLineBackgroundColorB = new Color(0.82f, 0.82f, 0.82f, 1.0f);
				private static readonly Color kKeyBackgroundColor = new Color(0.9f, 0.9f, 0.9f, 1.0f);
				private static readonly Color kTextBackgroundColorA = new Color(0.9f, 0.9f, 0.9f, 1.0f);
				private static readonly Color kTextBackgroundColorB = new Color(0.98f, 0.98f, 0.98f, 1.0f);

				private static LocalisationEditorWindow _instance = null;
				
				private LocalisationEditorPrefs _editorPrefs;
				private Rect _keysResizerRect;
				private Rect _languageResizerRect;

				private enum eResizing
				{
					NotResizing,
					ResizingKeys,
					ResizingLangauge,
				}

				private eResizing _resizing;
				private int _controlID;
				private float _resizingOffset;
				private Vector2 _scrollPosition;
				private bool _needsRepaint;
				private string _addNewKey = string.Empty;
				private bool _editingKeyName;
				private bool _wasEditingKeyName;
				private string _filter;

				private GUIStyle _toolbarStyle;
				private GUIStyle _titleStyle;
				private GUIStyle _keyStyle;
				private GUIStyle _keyEditStyle;
				private GUIStyle _textStyle;

				private int _viewStartIndex;
				private int _viewEndIndex;
				private float _contentHeight;
				private float _contentStart;

				private string[] _keys;

				private enum eKeySortOrder
				{
					None,
					Asc,
					Desc
				}
				private eKeySortOrder _sortOrder;

				//Havve key for glypths add button for each char in it at bottom which inserts them into local


				#region Menu Stuff
				[MenuItem("Window/Localisation Editor")]
				private static void CreateWindow()
				{
					// Get existing open window or if none, make a new one:
					_instance = (LocalisationEditorWindow)GetWindow(typeof(LocalisationEditorWindow), false, kWindowWindowName);
					_instance.Init();
				}
				#endregion

				#region EditorWindow
				void OnGUI()
				{
					CreateEditor();
					InitGUIStyles();

					_needsRepaint = false;


					EditorGUILayout.BeginVertical();
					{
						RenderTitleBar();
						RenderTable();
						RenderBottomBar();
					}
					EditorGUILayout.EndVertical();

					HandleInput();

					if (_needsRepaint)
						Repaint();
				}

				void OnDestroy()
				{
					if (Localisation.HasUnsavedChanges())
					{
						if (EditorUtility.DisplayDialog("Localisation Table Has Been Modified", "Do you want to save the changes you made to the table?\nYour changes will be lost if you don't save them.", "Save", "Don't Save"))
						{
							Localisation.SaveStrings();
						}
						else
						{
							Localisation.ReloadStrings();
						}
					}
				}
				#endregion

				public static void EditString(string key)
				{
					if (_instance == null)
						CreateWindow();

					_instance._filter = null;
					_instance.SelectKey(key);
				}

				private void CreateEditor()
				{
					if (_instance == null || _instance._editorPrefs == null)
					{
						_instance = (LocalisationEditorWindow)GetWindow(typeof(LocalisationEditorWindow), false, kWindowWindowName);
						_instance.Init();
					}
				}

				private void Init()
				{
					string editorPrefsText = ProjectEditorPrefs.GetString(kEditorPrefKey, "");
					try
					{
						_editorPrefs = Serializer.FromString<LocalisationEditorPrefs>(editorPrefsText);
					}
					catch
					{
						_editorPrefs = null;
					}

					if (_editorPrefs == null)
					{
						_editorPrefs = new LocalisationEditorPrefs();
					}

					_controlID = GUIUtility.GetControlID(FocusType.Passive);
				}

				private void InitGUIStyles()
				{
					if (_toolbarStyle == null || string.IsNullOrEmpty(_toolbarStyle.name))
					{
						_toolbarStyle = new GUIStyle(EditorStyles.toolbar)
						{
							padding = new RectOffset(4, 4, 0, 0),
						};
					}

					if (_titleStyle == null || string.IsNullOrEmpty(_titleStyle.name))
					{
						_titleStyle = new GUIStyle(EditorStyles.label)
						{
							richText = true,
							alignment = TextAnchor.MiddleCenter
						};
					}

					if (_keyStyle == null || string.IsNullOrEmpty(_keyStyle.name))
					{
						_keyStyle = new GUIStyle(EditorStyles.helpBox)
						{
							margin = new RectOffset(2, 0, 0, 0),
							fontSize = _editorPrefs._fontSize
						};
						_keyStyle.padding.left = 8;
					}

					if (_keyEditStyle == null || string.IsNullOrEmpty(_keyEditStyle.name))
					{
						_keyEditStyle = new GUIStyle(EditorStyles.textArea)
						{
							margin = new RectOffset(0, 0, 0, 0),
							fontSize = _editorPrefs._fontSize
						};
						_keyEditStyle.padding.left = 8;
						_keyEditStyle.padding.top = 3;
					}

					if (_textStyle == null || string.IsNullOrEmpty(_textStyle.name))
					{
						_textStyle = new GUIStyle(EditorStyles.textArea)
						{
							margin = new RectOffset(0, 1, 0, 1),
							font = _keyStyle.font,
							fontSize = _editorPrefs._fontSize,
							padding = new RectOffset(4, 4, 4, 4)
						};

						Font font = _editorPrefs._font.GetAsset();
						if (font == null)
							_textStyle.font = _keyStyle.font;
						else
							_textStyle.font = font;
					}
				}

				private void SaveEditorPrefs()
				{
					string prefsXml = Serializer.ToString(_editorPrefs);
					ProjectEditorPrefs.SetString(kEditorPrefKey, prefsXml);
				}

				private void RenderTitleBar()
				{
					EditorGUILayout.BeginVertical(GUILayout.Height(kToolBarHeight));
					{
						//Title
						EditorGUILayout.BeginHorizontal(_toolbarStyle);
						{
							string titleText = kWindowTitle + " - <b>Localisation.xml</b>";

							if (Localisation.HasUnsavedChanges())
								titleText += "<b>*</b>";

							EditorGUILayout.LabelField(titleText, _titleStyle);
						}
						EditorGUILayout.EndHorizontal();

						//Load save
						EditorGUILayout.BeginHorizontal(_toolbarStyle);
						{
							if (GUILayout.Button("Save", EditorStyles.toolbarButton))
							{
								Localisation.SaveStrings();
							}

							if (GUILayout.Button("Reload", EditorStyles.toolbarButton))
							{
								Localisation.ReloadStrings();
							}

							EditorGUILayout.Separator();

							GUILayout.Button("Scale", EditorStyles.toolbarButton);

							int fontSize = EditorGUILayout.IntSlider(_editorPrefs._fontSize, 8, 16);

							if (GUILayout.Button("Reset Scale", EditorStyles.toolbarButton))
							{
								fontSize = kDefaultFontSize;
								EditorGUI.FocusTextInControl(string.Empty);
							}

							if (_editorPrefs._fontSize != fontSize)
							{
								_editorPrefs._fontSize = fontSize;
								_keyStyle.fontSize = _editorPrefs._fontSize;
								_textStyle.fontSize = _editorPrefs._fontSize;
								SaveEditorPrefs();
							}

							EditorGUILayout.Separator();

							GUILayout.Button("Font", EditorStyles.toolbarButton);

							Font currentFont = _editorPrefs._font.GetAsset();
							Font font = (Font)EditorGUILayout.ObjectField(currentFont, typeof(Font), false);

							if (currentFont != font)
							{
								_editorPrefs._font = new EditorAssetRef<Font>(font);

								if (font == null)
									_textStyle.font = _keyStyle.font;
								else
									_textStyle.font = font;

								SaveEditorPrefs();
							}

							GUILayout.FlexibleSpace();
						}
						EditorGUILayout.EndHorizontal();

						//Filters
						EditorGUILayout.BeginHorizontal(_toolbarStyle);
						{
							GUILayout.Button("Filter", EditorStyles.toolbarButton);

							EditorGUI.BeginChangeCheck();
							_filter = EditorGUILayout.TextField(_filter);
							if (EditorGUI.EndChangeCheck())
							{
								_needsRepaint = true;
							}

							if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
							{
								_filter = "";
								SelectKey(_editorPrefs._selectedKey);
							}

							if (GUILayout.Button("Choose Localisation Folder", EditorStyles.toolbarButton))
							{
								string folder = LocalisationProjectSettings.Get()._localisationFolder;
								folder = EditorUtility.OpenFolderPanel("Choose Localisation Folder", folder, "");
								LocalisationProjectSettings.Get()._localisationFolder = AssetUtils.GetAssetPath(folder);
								AssetDatabase.SaveAssets();
								Localisation.ReloadStrings(true);
								_needsRepaint = true;
							}

							GUILayout.FlexibleSpace();
						}
						EditorGUILayout.EndHorizontal();

						//Headers
						EditorGUILayout.BeginHorizontal();
						{
							//Key
							EditorGUILayout.BeginHorizontal(_toolbarStyle, GUILayout.Width(_editorPrefs._keyWidth - 3));
							{
								if (GUILayout.Button("Key", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true)))
								{
									_sortOrder = _sortOrder == eKeySortOrder.Desc ? eKeySortOrder.Asc : eKeySortOrder.Desc;
								}
							}
							EditorGUILayout.EndHorizontal();

							//Keys Resizer
							RenderResizer(ref _keysResizerRect);

							//Text
							EditorGUILayout.BeginHorizontal(_toolbarStyle, GUILayout.Width(_editorPrefs._firstLanguageWidth - 3));
							{
								//First Language
								GUILayout.Button(Localisation.GetCurrentLanguage().ToString(), EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));
							}
							EditorGUILayout.EndHorizontal();

							//Language Resizer
							RenderResizer(ref _languageResizerRect);

							//Second Language
							float secondLangWidth = position.width - _editorPrefs._keyWidth - _editorPrefs._firstLanguageWidth;

							EditorGUILayout.BeginHorizontal(_toolbarStyle, GUILayout.Width(secondLangWidth));
							{
								EditorGUI.BeginChangeCheck();
								SystemLanguage language = (SystemLanguage)EditorGUILayout.EnumPopup(_editorPrefs._secondLanguage, EditorStyles.toolbarButton);
								if (EditorGUI.EndChangeCheck())
								{
									if (_editorPrefs._secondLanguage != Localisation.GetCurrentLanguage())
										Localisation.UnloadStrings(_editorPrefs._secondLanguage);

									_editorPrefs._secondLanguage = language;
								}
							}
							EditorGUILayout.EndHorizontal();
						}
						EditorGUILayout.EndHorizontal();
					}
					EditorGUILayout.EndVertical();
				}

				private void RenderResizer(ref Rect rect)
				{
					GUILayout.Box(string.Empty, _toolbarStyle, GUILayout.Width(4.0f), GUILayout.ExpandHeight(true));
					rect = GUILayoutUtility.GetLastRect();
					rect.x -= 8;
					rect.width += 16;
					EditorGUIUtility.AddCursorRect(rect, MouseCursor.SplitResizeLeftRight);
				}

				private void RenderTable()
				{
					Vector2 scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, false, false);
					{
						if (_scrollPosition != scrollPosition)
						{
							_scrollPosition = scrollPosition;
							EditorGUI.FocusTextInControl(string.Empty);
						}

						//On layout, check what part of table is currently being viewed
						if (Event.current.type == EventType.Layout)
						{
							_keys = GetKeys();
							GetViewableRange(_scrollPosition.y, GetTableAreaHeight(), out _viewStartIndex, out _viewEndIndex, out _contentStart, out _contentHeight);
						}
						
						EditorGUILayout.BeginVertical(GUILayout.Height(_contentHeight));
						{
							//Blank space until start of content
							GUILayout.Label(GUIContent.none, GUILayout.Height(_contentStart));
							
							//Then render viewable range
							for (int i = _viewStartIndex; i < _viewEndIndex; i++)
							{
								bool selected = _keys[i] == _editorPrefs._selectedKey;
									
								Color origBackgroundColor = GUI.backgroundColor;
								GUI.backgroundColor = selected ? kSelectedTextLineBackgroundColor : i % 2 == 0 ? kTextLineBackgroundColorA : kTextLineBackgroundColorB;
								
								float textHeight = GetItemHeight(_keys[i]);
								
								EditorGUILayout.BeginHorizontal(EditorUtils.ColoredRoundedBoxStyle, GUILayout.Height(textHeight));
								{
									GUI.backgroundColor = kKeyBackgroundColor;

									//Render Key
									EditorGUILayout.BeginVertical(GUILayout.Width(_editorPrefs._keyWidth));
									{
										if (selected)
										{
											if (_editingKeyName)
											{
												EditorGUI.BeginChangeCheck();
												GUI.SetNextControlName(kEditKeyId);
												string key = EditorGUILayout.DelayedTextField(_keys[i], _keyEditStyle, GUILayout.Width(_editorPrefs._keyWidth), GUILayout.ExpandHeight(true));
												if (EditorGUI.EndChangeCheck())
												{
													_editingKeyName = false;
													Localisation.ChangeKey(_keys[i], key);
												}

												if (!_wasEditingKeyName)
													EditorGUI.FocusTextInControl(kEditKeyId);

												_wasEditingKeyName = true;
											}
											else
											{
												if (GUILayout.Button(_keys[i], _keyStyle, GUILayout.Width(_editorPrefs._keyWidth), GUILayout.ExpandHeight(true)))
												{
													_editorPrefs._selectedKey = _keys[i];
													_editingKeyName = false;
													EditorGUI.FocusTextInControl(string.Empty);
												}
											}

											GUI.backgroundColor = kSelectedButtonsBackgroundColor;
											EditorGUILayout.BeginHorizontal(EditorUtils.ColoredRoundedBoxStyle);
											{
												GUI.backgroundColor = origBackgroundColor;

												if (GUILayout.Button("Edit Key", EditorStyles.toolbarButton))
												{
													_editingKeyName = true;
													_wasEditingKeyName = false;
												}

												if (GUILayout.Button("Delete", EditorStyles.toolbarButton))
												{
													DeleteSelected();
												}

												if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton))
												{
													DuplicateSelected();
												}

												GUILayout.FlexibleSpace();
											}
											EditorGUILayout.EndHorizontal();
										}
										//Not Selected
										else
										{
											if (GUILayout.Button(_keys[i], _keyStyle, GUILayout.Width(_editorPrefs._keyWidth), GUILayout.Height(textHeight)))
											{
												_editorPrefs._selectedKey = _keys[i];
												_editingKeyName = false;
												EditorGUI.FocusTextInControl(string.Empty);
											}
										}
									}
									EditorGUILayout.EndVertical();

									//Render Text
									GUI.backgroundColor = i % 2 == 0 ? kTextBackgroundColorA : kTextBackgroundColorB;

									//Render First Language
									{
										SystemLanguage language = Localisation.GetCurrentLanguage();

										EditorGUI.BeginChangeCheck();
										string text = Localisation.GetRawString(_keys[i], language);
										text = EditorGUILayout.TextArea(text, _textStyle, GUILayout.Width(_editorPrefs._firstLanguageWidth), GUILayout.Height(textHeight));
										if (EditorGUI.EndChangeCheck())
										{
											Localisation.Set(_keys[i], language, text);
										}
									}

									//Render Second Language
									{
										EditorGUI.BeginChangeCheck();
										string text = Localisation.GetRawString(_keys[i], _editorPrefs._secondLanguage);
										text = EditorGUILayout.TextArea(text, _textStyle, GUILayout.ExpandWidth(true), GUILayout.Height(textHeight));
										if (EditorGUI.EndChangeCheck())
										{
											Localisation.Set(_keys[i], _editorPrefs._secondLanguage, text);
										}
									}
								}
								EditorGUILayout.EndHorizontal();

								GUI.backgroundColor = origBackgroundColor;
							}
						}
						EditorGUILayout.EndVertical();
					}
					EditorGUILayout.EndScrollView();
				}

				private void RenderBottomBar()
				{
					EditorGUILayout.Separator();

					EditorGUILayout.BeginHorizontal(GUILayout.Height(kBottomBarHeight));
					{
						if (GUILayout.Button("Add New", EditorStyles.toolbarButton, GUILayout.Width(_editorPrefs._keyWidth)))
						{
							if (!Localisation.Exists(_addNewKey) && !string.IsNullOrEmpty(_addNewKey))
							{
								Localisation.Set(_addNewKey, Localisation.GetCurrentLanguage(), string.Empty);
								_keys = GetKeys();
								SelectKey(_addNewKey);
								_addNewKey = "";
							}
						}

						string[] folders = Localisation.GetStringFolders();
						int currentFolderIndex = 0;
						string keyWithoutFolder;
						Localisation.GetFolderIndex(_addNewKey, out currentFolderIndex, out keyWithoutFolder);

						EditorGUI.BeginChangeCheck();
						int newFolderIndex = EditorGUILayout.Popup(currentFolderIndex, folders);
						string currentFolder = newFolderIndex == 0 ? "" : folders[newFolderIndex];
						if (EditorGUI.EndChangeCheck())
						{
							if (newFolderIndex != 0)
								_addNewKey = currentFolder + "/" + keyWithoutFolder;
						}

						EditorGUILayout.LabelField("/", GUILayout.Width(8));

						if (newFolderIndex != 0)
						{
							keyWithoutFolder = EditorGUILayout.TextField(keyWithoutFolder);
							_addNewKey = currentFolder + "/" + keyWithoutFolder;
						}
						else
						{
							_addNewKey = EditorGUILayout.TextField(_addNewKey);
						}

						GUILayout.FlexibleSpace();
					}
					EditorGUILayout.EndHorizontal();

					EditorGUILayout.Separator();
				}

				private void HandleInput()
				{
					Event inputEvent = Event.current;

					if (inputEvent == null)
						return;

					EventType controlEventType = inputEvent.GetTypeForControl(_controlID);

					if (_resizing != eResizing.NotResizing && inputEvent.rawType == EventType.MouseUp)
					{
						_resizing = eResizing.NotResizing;
						_needsRepaint = true;
					}

					switch (controlEventType)
					{
						case EventType.MouseDown:
							{
								if (inputEvent.button == 0)
								{
									if (_keysResizerRect.Contains(inputEvent.mousePosition))
									{
										_resizing = eResizing.ResizingKeys;
									}
									else if(_languageResizerRect.Contains(inputEvent.mousePosition))
									{
										_resizing = eResizing.ResizingLangauge;
									}
									else
									{
										_resizing = eResizing.NotResizing;
									}

									if (_resizing != eResizing.NotResizing)
									{
										inputEvent.Use();
										_resizingOffset = inputEvent.mousePosition.x;
									}
								}
							}
							break;

						case EventType.MouseUp:
							{
								if (_resizing != eResizing.NotResizing)
								{
									inputEvent.Use();
									_resizing = eResizing.NotResizing;
								}
							}
							break;

						case EventType.MouseDrag:
							{
								if (_resizing != eResizing.NotResizing)
								{
									if (_resizing == eResizing.ResizingKeys)
									{
										_editorPrefs._keyWidth += (inputEvent.mousePosition.x - _resizingOffset);
										_editorPrefs._keyWidth = Math.Max(_editorPrefs._keyWidth, kMinKeysWidth);
									}
									else if (_resizing == eResizing.ResizingLangauge)
									{
										_editorPrefs._firstLanguageWidth += (inputEvent.mousePosition.x - _resizingOffset);
										_editorPrefs._firstLanguageWidth = Math.Max(_editorPrefs._firstLanguageWidth, kMinKeysWidth);
									}


									SaveEditorPrefs();
									_resizingOffset = inputEvent.mousePosition.x;
									_needsRepaint = true;
								}
							}
							break;

						case EventType.ValidateCommand:
							{
								if (inputEvent.commandName == "SoftDelete")
								{
									DeleteSelected();
								}
								else if (inputEvent.commandName == "Duplicate")
								{
									DuplicateSelected();
								}
								else if (inputEvent.commandName == "UndoRedoPerformed")
								{
									_needsRepaint = true;
								}
							}
							break;
					}
				}

				private void DeleteSelected()
				{
					if (!string.IsNullOrEmpty(_editorPrefs._selectedKey))
					{
						Localisation.Remove(_editorPrefs._selectedKey);
						_editorPrefs._selectedKey = null;
						SaveEditorPrefs();
						_needsRepaint = true;
					}
				}

				private void DuplicateSelected()
				{
					if (!string.IsNullOrEmpty(_editorPrefs._selectedKey))
					{ 
						string newKey = _editorPrefs._selectedKey + " (Copy)";

						//To do! Set text for all loaded languages?
						Localisation.Set(newKey, Localisation.GetCurrentLanguage(), Localisation.GetRawString(_editorPrefs._selectedKey, Localisation.GetCurrentLanguage()));
						_keys = GetKeys();
						SelectKey(newKey);

						_needsRepaint = true;
					}
				}
				
				private float GetItemHeight(string key)
				{
					float keyHeight = _keyStyle.fontSize;

					if (key == _editorPrefs._selectedKey)
					{
						keyHeight += kEditKeyBarHeight;
					}

					string textA = Localisation.GetRawString(key, Localisation.GetCurrentLanguage());
					float textAHeight = _textStyle.CalcSize(new GUIContent(textA)).y ;

					string textB = Localisation.GetRawString(key, _editorPrefs._secondLanguage);
					float textBHeight = _textStyle.CalcSize(new GUIContent(textB)).y ;

					return Mathf.Max(keyHeight, textAHeight, textBHeight);
				}	
				
				private void GetViewableRange(float viewStart, float viewHeight, out int startIndex, out int endIndex, out float contentStart, out float contentHeight)
				{
					startIndex = _keys.Length;
					endIndex = _keys.Length;
					contentStart = 0.0f;
					contentHeight = 0.0f;

					for (int i = 0; i < _keys.Length; i++)
					{
						float height = GetItemHeight(_keys[i]);

						if (viewStart >= contentHeight && viewStart <= contentHeight + height)
						{
							startIndex = i;
							contentStart = contentHeight;
						}

						contentHeight += height;

						if (contentHeight > viewStart + viewHeight && i < endIndex)
						{
							endIndex = i + 1;
						}
					}
				}

				private void SelectKey(string key)
				{
					Focus();

					InitGUIStyles();

					_keys = GetKeys();

					_editorPrefs._selectedKey = key;
					EditorGUI.FocusTextInControl(string.Empty);
					SaveEditorPrefs();

					_needsRepaint = true;
					
					float toSelected = 0.0f;
					bool foundKey = false;

					for (int i = 0; i < _keys.Length; i++)
					{
						foundKey = _keys[i] == _editorPrefs._selectedKey;

						if (foundKey)
							break;
						
						toSelected += GetItemHeight(_keys[i]);
					}

					if (foundKey)
					{
						float scrollAreaHeight = GetTableAreaHeight();
						_scrollPosition.y = Mathf.Max(toSelected - scrollAreaHeight * 0.4f, 0.0f);
					}
					else
					{
						_scrollPosition.y = 0.0f;
					}
				}

				private float GetTableAreaHeight()
				{
					return this.position.height - kToolBarHeight - kBottomBarHeight;
				}

				private string[] GetKeys()
				{
					string[] allKeys = Localisation.GetStringKeys();
					List<string> keys = new List<string>();

					for (int i = 1; i < allKeys.Length; i++)
					{
						if (MatchsFilter(allKeys[i]) || MatchsFilter(Localisation.GetRawString(allKeys[i], Localisation.GetCurrentLanguage())))
						{
							keys.Add(allKeys[i]);
						}
					}

					switch (_sortOrder)
					{
						case eKeySortOrder.Asc:
							keys.Sort(StringComparer.InvariantCulture);
							break;
						case eKeySortOrder.Desc:
							keys.Sort(StringComparer.InvariantCulture);
							keys.Reverse();
							break;
					}

					return keys.ToArray();
				}

				private bool MatchsFilter(string text)
				{
					if (!string.IsNullOrEmpty(_filter))
					{
						string textLow = text.ToLower();
						string[] words = _filter.Split(' ');

						foreach (string word in words)
						{
							if (textLow.Contains(word.ToLower()))
							{
								return true;
							}
						}

						return false;
					}

					return true;
				}
			}
		}
	}
}