/************************************************************************************
Filename    :   Win32_RoomTiny_Main.cpp
Content     :   First-person view test application for Oculus Rift
Created     :   11th May 2015
Authors     :   Tom Heath
Copyright   :   Copyright 2015 Oculus, Inc. All Rights reserved.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*************************************************************************************/
/// This is an entry-level sample, showing a minimal VR sample, 
/// in a simple environment.  Use WASD keys to move around, and cursor keys.
/// Dismiss the health and safety warning by tapping the headset, 
/// or pressing any key. 
/// It runs with DirectX11.

// Include DirectX
#include "Win32_DirectXAppUtil.h"

// Include the Oculus SDK
#include "OVR_CAPI_D3D.h"

#include "tchar.h"
#include "time.h"

//#pragma comment(lib, "LibOVR.lib")

//------------------------------------------------------------
// ovrSwapTextureSet wrapper class that also maintains the render target views
// needed for D3D11 rendering.
struct OculusTexture
{
    ovrSession               Session;
	ovrTextureSwapChain      TextureChain;
    std::vector<ID3D11RenderTargetView*> TexRtv;

    OculusTexture() :
        Session(nullptr),
        TextureChain(nullptr)
    {
    }

    bool Init(ovrSession session, int sizeW, int sizeH)
	{
        Session = session;

        ovrTextureSwapChainDesc desc = {};
        desc.Type = ovrTexture_2D;
        desc.ArraySize = 1;
        desc.Format = OVR_FORMAT_R8G8B8A8_UNORM_SRGB;
        desc.Width = sizeW;
        desc.Height = sizeH;
        desc.MipLevels = 1;
        desc.SampleCount = 1;
        desc.MiscFlags = ovrTextureMisc_DX_Typeless;
        desc.BindFlags = ovrTextureBind_DX_RenderTarget;
        desc.StaticImage = ovrFalse;

        ovrResult result = ovr_CreateTextureSwapChainDX(session, DIRECTX.Device, &desc, &TextureChain);
        if (!OVR_SUCCESS(result))
            return false;

        int textureCount = 0;
        ovr_GetTextureSwapChainLength(Session, TextureChain, &textureCount);
		for (int i = 0; i < textureCount; ++i)
		{
            ID3D11Texture2D* tex = nullptr;
            ovr_GetTextureSwapChainBufferDX(Session, TextureChain, i, IID_PPV_ARGS(&tex));
			D3D11_RENDER_TARGET_VIEW_DESC rtvd = {};
			rtvd.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
			rtvd.ViewDimension = D3D11_RTV_DIMENSION_TEXTURE2D;
            ID3D11RenderTargetView* rtv;
			DIRECTX.Device->CreateRenderTargetView(tex, &rtvd, &rtv);
            TexRtv.push_back(rtv);
            tex->Release();
		}

        return true;
    }

	~OculusTexture()
	{
		for (int i = 0; i < (int)TexRtv.size(); ++i)
        {
            Release(TexRtv[i]);
        }
		if (TextureChain)
        {
            ovr_DestroyTextureSwapChain(Session, TextureChain);
        }
	}

    ID3D11RenderTargetView* GetRTV()
	{
        int index = 0;
        ovr_GetTextureSwapChainCurrentIndex(Session, TextureChain, &index);
        return TexRtv[index];
    }

    // Commit changes
	void Commit()
	{
        ovr_CommitTextureSwapChain(Session, TextureChain);
	}
};

#include "resource.h"
#include "Prompt.h"

// return true to retry later (e.g. after display lost)
static bool MainLoop(bool retryCreate)
{
    // Initialize these to nullptr here to handle device lost failures cleanly
	ovrMirrorTexture mirrorTexture = nullptr;
	OculusTexture  * pEyeRenderTexture[2] = { nullptr, nullptr };
	DepthBuffer    * pEyeDepthBuffer[2] = { nullptr, nullptr };
    Scene          * roomScene = nullptr; 
    Camera         * mainCam = nullptr;
	ovrMirrorTextureDesc mirrorDesc = {};
    bool isVisible       = true;
    long long frameIndex = 0;
	bool doneInit = false;

	Prompt messageTexture;

	ovrSession session;
	ovrGraphicsLuid luid;
    ovrResult result = ovr_Create(&session, &luid);
    if (!OVR_SUCCESS(result))
        return retryCreate;

    ovrHmdDesc hmdDesc = ovr_GetHmdDesc(session);

	// Setup Device and Graphics
	// Note: the mirror window can be any size, for this sample we use 1/2 the HMD resolution
    if (!DIRECTX.InitDevice(hmdDesc.Resolution.w / 2, hmdDesc.Resolution.h / 2, reinterpret_cast<LUID*>(&luid)))
        goto Done;


	// Make the eye render buffers (caution if actual size < requested due to HW limits). 
	ovrRecti         eyeRenderViewport[2];

	for (int eye = 0; eye < 2; ++eye)
	{
		ovrSizei idealSize = ovr_GetFovTextureSize(session, (ovrEyeType)eye, hmdDesc.DefaultEyeFov[eye], 1.0f);
		pEyeRenderTexture[eye] = new OculusTexture();
        if (!pEyeRenderTexture[eye]->Init(session, idealSize.w, idealSize.h))
        {
            if (retryCreate) goto Done;
	        VALIDATE(OVR_SUCCESS(result), "Failed to create eye texture.");
        }
		pEyeDepthBuffer[eye] = new DepthBuffer(DIRECTX.Device, idealSize.w, idealSize.h);
		eyeRenderViewport[eye].Pos.x = 0;
		eyeRenderViewport[eye].Pos.y = 0;
		eyeRenderViewport[eye].Size = idealSize;
        if (!pEyeRenderTexture[eye]->TextureChain)
        {
            if (retryCreate) goto Done;
            VALIDATE(false, "Failed to create texture.");
        }
	}

	// Create a mirror to see on the monitor.
    mirrorDesc.Format = OVR_FORMAT_R8G8B8A8_UNORM_SRGB;
    mirrorDesc.Width = DIRECTX.WinSizeW;
    mirrorDesc.Height = DIRECTX.WinSizeH;
    result = ovr_CreateMirrorTextureDX(session, DIRECTX.Device, &mirrorDesc, &mirrorTexture);
    if (!OVR_SUCCESS(result))
    {
        if (retryCreate) goto Done;
        VALIDATE(false, "Failed to create mirror texture.");
    }

	// Create camera
    mainCam = new Camera(&XMVectorSet(0.0f, 0.0f, 5.0f, 0), &XMQuaternionIdentity());

    // FloorLevel will give tracking poses where the floor height is 0
    ovr_SetTrackingOriginType(session, ovrTrackingOrigin_FloorLevel);

	ovrPosef zeroPose;
	zeroPose.Position.x = 0;
	zeroPose.Position.y = 0;
	zeroPose.Position.z = 0;
	zeroPose.Orientation.w = 1;
	zeroPose.Orientation.x = 0;
	zeroPose.Orientation.y = 0;
	zeroPose.Orientation.z = 0;

	// INITIALISE MESSAGE TEXTURE-------------------------------------------------
	Prompt::ResourceID id = Prompt::ID_UNKNOWN;
	if (__argc>1)
	{
		char idname[256];
		int i=0;
		for (i = 0; __argv[1][i]!=0; ++i)
		{
			idname[i] = toupper(__argv[1][i]);
		}
		idname[i] = 0;
		id = messageTexture.IDFromName(idname);
	}
	messageTexture.Init(id , DIRECTX.Device);
	switch(id)
	{
	case Prompt::ID_UNKNOWN:
		{
			::SetWindowText(DIRECTX.Window, _T("Close window to continue."));
			break;
		}
	case Prompt::REG:
		{
			::SetWindowText(DIRECTX.Window, _T("Close window and restart game after completing registration."));
			break;
		}
	case Prompt::LOGIN:
		{
			::SetWindowText(DIRECTX.Window, _T("Close window and login via the Elite Dangerous launcher."));
			break;
		}
	}
	Texture* messageTex = new Texture();
	ID3D11Texture2D* messageResource = messageTexture.GetTexture();
	if (messageResource != NULL)
	{
		unsigned int dimension = 1024;
		messageTex->SizeH = dimension;
		messageTex->SizeW = dimension;
		messageTex->MipLevels = 1;
		messageTex->Tex = messageResource;
		ID3D11RenderTargetView* rtv;
		D3D11_RENDER_TARGET_VIEW_DESC rtvd = {};
		rtvd.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
		rtvd.ViewDimension = D3D11_RTV_DIMENSION_TEXTURE2D;
		HRESULT r = DIRECTX.Device->CreateRenderTargetView(messageResource, NULL, &rtv);
		if (r != S_OK) { goto Done; }

		messageTex->TexRtv = rtv;
		ID3D11Resource* target;
		messageTex->TexRtv->GetResource(&target);
		HRESULT s = DIRECTX.Device->CreateShaderResourceView(target, NULL, &messageTex->TexSv);
		if (s != S_OK) { goto Done; }

		// Create the room model
		roomScene = new Scene();
		TriangleSet message;
		message.AddQuad
		(
			Vertex(XMFLOAT3(-5.0f, -5.0f, -5.0f), 0xFFFFFFFF, 0.0f, 1.0f),
			Vertex(XMFLOAT3(-5.0f, 5.0f, -5.0f), 0xFFFFFFFF, 0.0f, 0.0f),
			Vertex(XMFLOAT3(5.0f, -5.0f, -5.0f), 0xFFFFFFFF, 1.0f, 1.0f),
			Vertex(XMFLOAT3(5.0f, 5.0f, -5.0f), 0xFFFFFFFF, 1.0f, 0.0f)
		);
		roomScene->Add
		(
			new Model
			(
				&message,
				XMFLOAT3(0, 0, 0),
				XMFLOAT4(0, 0, 0, 1),
				new Material(messageTex)
			)
		);
	}
	//----------------------------------------------------------------------

	time_t endtime = time(nullptr) + 20;
	time_t currentTime = time(nullptr);
	doneInit = true;

	// Main loop
	while (DIRECTX.HandleMessages())
	{
		// Animate the cube
		//static float cubeClock = 0;
		//roomScene->Models[0]->Pos = XMFLOAT3(9 * sin(cubeClock), 3, 9 * cos(cubeClock += 0.015f));

	    // Call ovr_GetRenderDesc each frame to get the ovrEyeRenderDesc, as the returned values (e.g. HmdToEyeOffset) may change at runtime.
	    ovrEyeRenderDesc eyeRenderDesc[2];
	    eyeRenderDesc[0] = ovr_GetRenderDesc(session, ovrEye_Left, hmdDesc.DefaultEyeFov[0]);
	    eyeRenderDesc[1] = ovr_GetRenderDesc(session, ovrEye_Right, hmdDesc.DefaultEyeFov[1]);

		// Get both eye poses simultaneously, with IPD offset already included. 
		ovrPosef         EyeRenderPose[2];
        ovrVector3f      HmdToEyeOffset[2] = { eyeRenderDesc[0].HmdToEyeOffset,
			                                   eyeRenderDesc[1].HmdToEyeOffset };

        double sensorSampleTime;    // sensorSampleTime is fed into the layer later
        ovr_GetEyePoses(session, frameIndex, ovrTrue, HmdToEyeOffset, EyeRenderPose, &sensorSampleTime);

		// Render Scene to Eye Buffers
        if (isVisible)
        {
            for (int eye = 0; eye < 2; ++eye)
		    {
			    // Clear and set up rendertarget
			    DIRECTX.SetAndClearRenderTarget(pEyeRenderTexture[eye]->GetRTV(), pEyeDepthBuffer[eye]);
			    DIRECTX.SetViewport((float)eyeRenderViewport[eye].Pos.x, (float)eyeRenderViewport[eye].Pos.y,
				                    (float)eyeRenderViewport[eye].Size.w, (float)eyeRenderViewport[eye].Size.h);

			    //Get the pose information in XM format
			    XMVECTOR eyeQuat = XMVectorSet(EyeRenderPose[eye].Orientation.x, EyeRenderPose[eye].Orientation.y,
				                               EyeRenderPose[eye].Orientation.z, EyeRenderPose[eye].Orientation.w);
			    XMVECTOR eyePos = XMVectorSet(EyeRenderPose[eye].Position.x, EyeRenderPose[eye].Position.y, EyeRenderPose[eye].Position.z, 0);

			    // Get view and projection matrices for the Rift camera
			    XMVECTOR CombinedPos = XMVectorAdd(mainCam->Pos, XMVector3Rotate(eyePos, mainCam->Rot));
			    Camera finalCam(&CombinedPos, &(XMQuaternionMultiply(eyeQuat,mainCam->Rot)));
			    XMMATRIX view = finalCam.GetViewMatrix();
                ovrMatrix4f p = ovrMatrix4f_Projection(eyeRenderDesc[eye].Fov, 0.2f, 1000.0f, ovrProjection_None);
			    XMMATRIX proj = XMMatrixSet(p.M[0][0], p.M[1][0], p.M[2][0], p.M[3][0],
				                            p.M[0][1], p.M[1][1], p.M[2][1], p.M[3][1],
				                            p.M[0][2], p.M[1][2], p.M[2][2], p.M[3][2],
				                            p.M[0][3], p.M[1][3], p.M[2][3], p.M[3][3]);
			    XMMATRIX prod = XMMatrixMultiply(view, proj);
			    roomScene->Render(&prod, 1, 1, 1, 1, true);

                // Commit rendering to the swap chain
                pEyeRenderTexture[eye]->Commit();
		    }
        }

		// Initialize our single full screen Fov layer.
		ovrLayerHeader* layers[2];
        ovrLayerEyeFov ld = {};
		ld.Header.Type = ovrLayerType_EyeFov;
		ld.Header.Flags = 0;

		for (int eye = 0; eye < 2; ++eye)
		{
			ld.ColorTexture[eye] = pEyeRenderTexture[eye]->TextureChain;
			ld.Viewport[eye] = eyeRenderViewport[eye];
			ld.Fov[eye] = hmdDesc.DefaultEyeFov[eye];
			ld.RenderPose[eye] = EyeRenderPose[eye];
            ld.SensorSampleTime = sensorSampleTime;
		}

        layers[0] = &ld.Header;
        result = ovr_SubmitFrame(session, frameIndex, nullptr, &layers[0], 1);
        // exit the rendering loop if submit returns an error, will retry on ovrError_DisplayLost
        if (!OVR_SUCCESS(result))
            goto Done;

        isVisible = (result == ovrSuccess);

        ovrSessionStatus sessionStatus;
        ovr_GetSessionStatus(session, &sessionStatus);
        if (sessionStatus.ShouldQuit)
            goto Done;
        if (sessionStatus.ShouldRecenter)
            ovr_RecenterTrackingOrigin(session);

        // Render mirror
        ID3D11Texture2D* tex = nullptr;
        ovr_GetMirrorTextureBufferDX(session, mirrorTexture, IID_PPV_ARGS(&tex));
        DIRECTX.Context->CopyResource(DIRECTX.BackBuffer, tex);
        tex->Release();
        DIRECTX.SwapChain->Present(0, 0);

        frameIndex++;

		currentTime = time(nullptr);
		if (currentTime>=endtime)
		{
			// Timed out, do not restart
			DIRECTX.Running = false;
		}
	}

	// Release resources
Done:
    delete mainCam;
	if(roomScene)
	{
		delete roomScene;
	}
	if (mirrorTexture)
        ovr_DestroyMirrorTexture(session, mirrorTexture);
    for (int eye = 0; eye < 2; ++eye)
    {
	    delete pEyeRenderTexture[eye];
        delete pEyeDepthBuffer[eye];
    }
	DIRECTX.ReleaseDevice();
	ovr_Destroy(session);

    // Retry on ovrError_DisplayLost
    return retryCreate || OVR_SUCCESS(result) || (result == ovrError_DisplayLost);
}

//-------------------------------------------------------------------------------------
int WINAPI WinMain(HINSTANCE hinst, HINSTANCE, LPSTR, int)
{
	// Initializes LibOVR, and the Rift
	ovrResult result = ovr_Initialize(nullptr);
	VALIDATE(OVR_SUCCESS(result), "Failed to initialize libOVR.");

    VALIDATE(DIRECTX.InitWindow(hinst, L"ORPrompt"), "Failed to open window.");

    DIRECTX.Run(MainLoop);

	ovr_Shutdown();
	return(0);
}
