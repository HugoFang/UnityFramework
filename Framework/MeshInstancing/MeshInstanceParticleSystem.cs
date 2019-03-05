﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Framework
{
	using Maths;

	namespace MeshInstancing
	{
		[RequireComponent(typeof(ParticleSystem))]
		public class MeshInstanceParticleSystem : MonoBehaviour
		{
			#region Public Data
			public Mesh _mesh;
			public Material[] _materials;
			public Vector3 _meshOffset = Vector3.zero;
			public Vector3 _meshScale = Vector3.one;
			public ShadowCastingMode _shadowCastingMode;

			public enum eRotationType
			{
				FromParticle,
				Billboard,
				AlignWithVelocity,
			}
			public eRotationType _particleRotation;
			public bool _sortByDepth;
			public bool _frustrumCull;
			public float _boundRadius;
			public float _frustrumPadding;
			#endregion

			#region Private Data
			private class ParticleData
			{
				public int _index;
				public Matrix4x4 _transform;
				public float _zDist;
			}
			protected ParticleSystem _particleSystem;
			protected ParticleSystem.Particle[] _particles;
			protected MaterialPropertyBlock _propertyBlock;
			private ParticleData[] _particleData;
			private List<ParticleData> _renderedParticles;
			private Matrix4x4[] _particleTransforms;
			#endregion

			#region Monobehaviour
			protected virtual void Update()
			{
				InitialiseIfNeeded();
				Render(Camera.main);
			}
			#endregion
			
			#region Protected Functions
			protected virtual void InitialiseIfNeeded()
			{
				if (_propertyBlock == null)
				{
					_propertyBlock = new MaterialPropertyBlock();
				}

				if (_particleSystem == null || _particles == null)
				{
					_particleSystem = GetComponent<ParticleSystem>();
					_particles = new ParticleSystem.Particle[_particleSystem.main.maxParticles];
					_particleTransforms = new Matrix4x4[Math.Min(_particleSystem.main.maxParticles, 1023)];
					_particleData = new ParticleData[_particleTransforms.Length];
					for (int i = 0; i < _particleTransforms.Length; i++)
						_particleData[i] = new ParticleData();
					_renderedParticles = new List<ParticleData>(_particleTransforms.Length);
				}
			}

			protected virtual void UpdateProperties()
			{

			}

			protected int GetNumRenderedParticles()
			{
				return _renderedParticles.Count;
			}

			protected int GetRenderedParticlesIndex(int i)
			{
				return _renderedParticles[i]._index;
			}

			protected void Render(Camera camera)
			{
				if (_mesh == null || _materials.Length < _mesh.subMeshCount)
					return;

				int numAlive = _particleSystem.GetParticles(_particles);
				_renderedParticles.Clear();

				if (numAlive > 0)
				{
					Plane[] planes = null;

					if (_frustrumCull)
						planes = GeometryUtility.CalculateFrustumPlanes(camera);

					int numParticles = Math.Min(numAlive, _particleTransforms.Length);

					for (int i = 0; i < numParticles; i++)
					{
						Quaternion rot;

						switch (_particleRotation)
						{
							case eRotationType.AlignWithVelocity:
								{
									Vector3 foward = _particles[i].velocity;
									rot = Quaternion.LookRotation(foward);
								}
								break;
							case eRotationType.Billboard:
								{
									Vector3 forward = _particles[i].position - camera.transform.position;
									Vector3 left = Vector3.Cross(forward, Vector3.up);
									Vector3 up = Quaternion.AngleAxis(_particles[i].rotation, forward) * Vector3.Cross(left, forward);
									rot = Quaternion.LookRotation(forward, up);
								}
								break;
							case eRotationType.FromParticle:
							default:
								{
									rot = Quaternion.AngleAxis(_particles[i].rotation, _particles[i].axisOfRotation);
								}
								break;
						}

						Vector3 scale = Vector3.Scale(_particles[i].GetCurrentSize3D(_particleSystem), _meshScale);

						Vector3 pos = _particles[i].position + rot * _meshOffset;

						bool rendered = true;

						//If frustum culling is enabled, check should draw this particle
						if (_frustrumCull)
						{
							rendered = MathUtils.IsSphereInFrustrum(ref planes, ref pos, _boundRadius * Mathf.Max(scale.x, scale.y, scale.z), _frustrumPadding);
						}

						if (rendered)
						{
							_particleData[i]._index = i;
							_particleData[i]._transform.SetTRS(pos, rot, scale);

							if (_sortByDepth)
							{
								_particleData[i]._zDist = (camera.transform.position - pos).sqrMagnitude;
							}

							AddToSortedList(ref _particleData[i]);
						}
					}

					if (_renderedParticles.Count > 0)
					{
						UpdateProperties();
						FillTransformMatricies();

						for (int i = 0; i < _mesh.subMeshCount; i++)
						{
							Graphics.DrawMeshInstanced(_mesh, i, _materials[i], _particleTransforms, _renderedParticles.Count, _propertyBlock, _shadowCastingMode);
						}
					}
				}
			}
			#endregion

			#region Private Functions
			private void FillTransformMatricies()
			{
				for (int i = 0; i < _renderedParticles.Count; i++)
				{
					_particleTransforms[i] = _renderedParticles[i]._transform;
				}
			}

			private void AddToSortedList(ref ParticleData particleData)
			{
				int index = 0;

				if (_sortByDepth)
				{
					index = FindInsertIndex(particleData._zDist, 0, _renderedParticles.Count);
				}	

				_renderedParticles.Insert(index, particleData);
			}

			private static readonly int kSearchNodes = 24;

			private int FindInsertIndex(float zDist, int startIndex, int endIndex)
			{
				int searchWidth = endIndex - startIndex;
				int numSearches = Mathf.Min(kSearchNodes, searchWidth);
				int nodesPerSearch = Mathf.FloorToInt(searchWidth / (float)numSearches);

				int currIndex = startIndex;
				int prevIndex = currIndex;

				for (int i =0; i<numSearches; i++)
				{
					//If this distance is greater than current node its between this and prev node
					if (zDist > _renderedParticles[currIndex]._zDist)
					{
						//If first node or search one node at a time then found our index
						if (i == 0  || nodesPerSearch == 1)
						{
							return currIndex;
						}
						//Otherwise its between this and the previous index
						else
						{
							return FindInsertIndex(zDist, prevIndex, currIndex);
						}
					}

					prevIndex = currIndex;
					currIndex = (i == numSearches - 1) ? endIndex : startIndex + ((i + 1) * nodesPerSearch);
				}

				return endIndex;
			}
			#endregion
		}
	}
}
