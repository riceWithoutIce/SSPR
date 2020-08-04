using System.Collections;
using System.Collections.Generic;
// using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Rendering;

public class ScreenSpacePlanarReflection : MonoBehaviour
{
	private const int ThreadCount = 32;
	private readonly int HashTex = Shader.PropertyToID("_HashResult");
	private readonly int RefTex = Shader.PropertyToID("_RefTex");

	// compute shader
	public ComputeShader CSSSPR;
	public Shader ShaderSSPR;
	// plane
	public Transform Plane;

//	[Header("Down Sample")]
//	[Range(0.6f, 1.0f)]public float DownSample = 1;

	// mat
	private Material _matSSPR;

	// camera
	private Camera _cam;
	

	// cmd
	private CommandBuffer _cmdHash;

	private Matrix4x4 _x4InvVP;
	private Matrix4x4 _x4VP;

	private Vector2Int _size;
	private Vector2Int _threadSize;

    // Start is called before the first frame update
    void Start()
    {
	    _cam = Camera.main;
	    _cam.depthTextureMode = DepthTextureMode.Depth;
	    _size.x = (int)(_cam.pixelWidth);
	    _size.y = (int)(_cam.pixelHeight);
	    _threadSize.x = _size.x / ThreadCount + (_size.x % 32 > 0 ? 1 : 0);
	    _threadSize.y = _size.y / ThreadCount + (_size.y % 32 > 0 ? 1 : 0);

	    InitMaterial();
		InitCmd();
		InitCompute();
    }

	private void InitMaterial()
	{
		if (_matSSPR == null)
		{
			_matSSPR = new Material(ShaderSSPR);
		}
	}

	private void InitCompute()
	{
		_x4VP = new Matrix4x4();
		_x4InvVP = new Matrix4x4();
	}

	private void InitCmd()
	{
		if (_cam == null)
			return;

		if (_cmdHash == null)
		{
			SetCmd();
			_cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, _cmdHash);
		}
	}

	private void SetCmd()
	{
		RenderTextureDescriptor rtRef = new RenderTextureDescriptor();
		rtRef.width = _size.x;
		rtRef.height = _size.y;
		rtRef.enableRandomWrite = true;
		rtRef.msaaSamples = 1;
		rtRef.dimension = TextureDimension.Tex2D;
		rtRef.colorFormat = RenderTextureFormat.DefaultHDR;

		RenderTextureDescriptor rtHash = new RenderTextureDescriptor();
		rtHash.width = _size.x;
		rtHash.height = _size.y;
		rtHash.enableRandomWrite = true;
		rtHash.msaaSamples = 1;
		rtHash.dimension = TextureDimension.Tex2D;
		rtHash.colorFormat = RenderTextureFormat.RInt;

		_cmdHash = new CommandBuffer() { name = "SSPR" };
		_cmdHash.GetTemporaryRT(RefTex, rtRef);
		_cmdHash.GetTemporaryRT(HashTex, rtHash);

		// clear
		var kernelClear = CSSSPR.FindKernel("SSPR_Clear");
		_cmdHash.SetComputeIntParams(CSSSPR, "_Size", _size.x, _size.y);
		_cmdHash.SetComputeTextureParam(CSSSPR, kernelClear, "_HashClearTex", HashTex);
		_cmdHash.DispatchCompute(CSSSPR, kernelClear, _threadSize.x, _threadSize.y, 1);

		// hash
		var kernelHash = CSSSPR.FindKernel("SSPR_Hash");
		_cmdHash.SetComputeTextureParam(CSSSPR, kernelHash, "_HashResult", HashTex);
		Matrix4x4 view = _cam.worldToCameraMatrix;
		Matrix4x4 proj = GL.GetGPUProjectionMatrix(_cam.projectionMatrix, true);
		_x4VP = proj * view;
		_x4InvVP = _x4VP.inverse;

		_cmdHash.SetComputeMatrixParam(CSSSPR, "_IVP", _x4InvVP);
		_cmdHash.SetComputeMatrixParam(CSSSPR, "_VP", _x4VP);

		// reflect info
		Vector3 rot = Plane.rotation * Vector3.up;
		rot.Normalize();
		Vector4 reflectData = new Vector4();
		reflectData.x = rot.x;
		reflectData.y = rot.y;
		reflectData.z = rot.z;
		reflectData.w = -Vector3.Dot(rot, Plane.position);
		_cmdHash.SetComputeVectorParam(CSSSPR, "_ReflectData", reflectData);

		_cmdHash.DispatchCompute(CSSSPR, kernelHash, _threadSize.x, _threadSize.y, 1);

		// hash resolve
		var kernelHashResolve = CSSSPR.FindKernel("SSPR_Hash_Resolve");
		_cmdHash.SetComputeTextureParam(CSSSPR, kernelHashResolve, "_HashTex", HashTex);
		_cmdHash.SetComputeTextureParam(CSSSPR, kernelHashResolve, "_ColorTex", BuiltinRenderTextureType.CurrentActive);
		_cmdHash.SetComputeTextureParam(CSSSPR, kernelHashResolve, "_RefTex", RefTex);

		_cmdHash.DispatchCompute(CSSSPR, kernelHashResolve, _threadSize.x, _threadSize.y, 1);
	}

	private void Update()
	{
		// UpdateCmd();
	}

	private void UpdateCmd()
	{
		if (_cmdHash == null)
			return;

		SetCmd();
	}

	private void OnDisable()
	{
		if (_matSSPR)
		{
			GameObject.Destroy(_matSSPR);
			_matSSPR = null;
		}

		if (_cmdHash != null)
		{
			_cam.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, _cmdHash);
			_cmdHash.Clear();
			_cmdHash = null;
		}
	}
}
