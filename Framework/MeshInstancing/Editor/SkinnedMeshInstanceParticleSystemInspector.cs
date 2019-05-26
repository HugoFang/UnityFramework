﻿using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Framework
{
	namespace MeshInstancing
	{
		using GPUAnimations;

		namespace Editor
		{
			[CustomEditor(typeof(SkinnedMeshInstanceParticleSystem), true)]
			public class SkinnedMeshInstanceParticleSystemInspector : UnityEditor.Editor
			{
				private ReorderableList _animationList;

				void OnEnable()
				{
					_animationList = new ReorderableList(new SkinnedMeshInstanceParticleSystem.ParticleAnimation[0], typeof(SkinnedMeshInstanceParticleSystem.ParticleAnimation), false, true, true, true)
					{
						drawHeaderCallback = new ReorderableList.HeaderCallbackDelegate(OnDrawHeader),
						drawElementCallback = new ReorderableList.ElementCallbackDelegate(OnDrawAnimationItem),
						showDefaultBackground = true,
						index = 0,
						elementHeight = 20f
					};
				}

				public override void OnInspectorGUI()
				{
					base.DrawDefaultInspector();
					
					SkinnedMeshInstanceParticleSystem particleSystem = target as SkinnedMeshInstanceParticleSystem;

					EditorGUI.BeginChangeCheck();

					GUILayout.Label("Animations", EditorStyles.boldLabel);
					GUILayout.Space(3f);
					_animationList.list = new List<SkinnedMeshInstanceParticleSystem.ParticleAnimation>(particleSystem._animations);
					_animationList.DoLayoutList();

					if (EditorGUI.EndChangeCheck())
					{
						Undo.RecordObject(target, "Changed Animations");

						particleSystem._animations = new SkinnedMeshInstanceParticleSystem.ParticleAnimation[_animationList.list.Count];
						for (int i = 0; i < particleSystem._animations.Length; i++)
							particleSystem._animations[i] = (SkinnedMeshInstanceParticleSystem.ParticleAnimation)_animationList.list[i];
					}
				}

				protected virtual void OnDrawHeader(Rect rect)
				{
					float columnWidth = rect.width /= 3f;
					GUI.Label(rect, "Animation", EditorStyles.label);
					rect.x += columnWidth;
					GUI.Label(rect, "Probability", EditorStyles.label);
					rect.x += columnWidth;
					GUI.Label(rect, "Speed Range", EditorStyles.label);
				}

				private void OnDrawAnimationItem(Rect rect, int index, bool selected, bool focused)
				{
					SkinnedMeshInstanceParticleSystem particleSystem = target as SkinnedMeshInstanceParticleSystem;
					SkinnedMeshInstanceParticleSystem.ParticleAnimation animation = (SkinnedMeshInstanceParticleSystem.ParticleAnimation)_animationList.list[index];
					
					float columnWidth = rect.width / 3f;
					rect.width = columnWidth;

					GPUAnimations.GPUAnimations.Animation[] animations = particleSystem._animationTexture.GetAnimations();

					GUIContent[] animNames = new GUIContent[animations.Length];
					for (int i = 0; i < animNames.Length; i++)
						animNames[i] = new GUIContent(animations[i]._name);

					animation._animationIndex = EditorGUI.Popup(rect, animation._animationIndex, animNames);

					rect.x += columnWidth;
					animation._probability = EditorGUI.Slider(rect, animation._probability, 0f, 1f);

					rect.x += columnWidth;
					{
						rect.width = columnWidth * 0.5f;
						animation._speedRange._min = EditorGUI.FloatField(rect, animation._speedRange._min);
						rect.x += rect.width;
						animation._speedRange._max = EditorGUI.FloatField(rect, animation._speedRange._max);
					}
					
					_animationList.list[index] = animation;
				}
			}
		}
	}
}
