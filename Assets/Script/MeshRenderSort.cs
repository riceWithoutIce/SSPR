using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MeshRenderSort : MonoBehaviour
{
	public int SortingOrder;

	private MeshRenderer _renderer;

	private void Start()
	{
		_renderer = GetComponent<MeshRenderer>();
	}

	private void Update()
	{
		if (_renderer != null)
		{
			_renderer.sortingOrder = SortingOrder;
		}
	}
}
