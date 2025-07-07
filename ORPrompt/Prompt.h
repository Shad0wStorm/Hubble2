class Prompt {
	ID3D11Texture2D* m_texture;
public:
	enum ResourceID {
		LOGIN,
		REG,
		UPGRADE,
		ID_UNKNOWN,
	};
	Prompt()
	{
		m_texture = NULL;
	};
		ResourceID IDFromName(const char* name)
	{
		if (strcmp(name,"LOGIN")==0)
		{
			return LOGIN;
		}
		if (strcmp(name,"REG")==0)
		{
			return REG;
		}
		if (strcmp(name,"UPGRADE")==0)
		{
			return UPGRADE;
		}
		if (strcmp(name,"ID_UNKNOWN")==0)
		{
			return ID_UNKNOWN;
		}
		return ID_UNKNOWN;
	}
	void Init(ResourceID id, ID3D11Device* device)
	{
		const int dimension = 1024;
		const int buffersize = 4 * dimension * dimension;
		HRSRC resourceHandle = ::FindResource(NULL, MAKEINTRESOURCE(IDR_RCDATA1), RT_RCDATA);
		HGLOBAL resourceData = ::LoadResource(NULL, resourceHandle);
		char* rawData = (char*)::LockResource(resourceData);
		D3D11_SUBRESOURCE_DATA imageData;
		switch (id)
		{
			case LOGIN:
				{
					D3D11_TEXTURE2D_DESC desc;
					desc.Width = 1024;
					desc.Height = 1024;
					desc.MipLevels = 1;
					desc.ArraySize = 1;
					desc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
					desc.SampleDesc.Count = 1;
					desc.SampleDesc.Quality = 0;
					desc.Usage = D3D11_USAGE_DEFAULT;
					desc.CPUAccessFlags = 0;
					desc.MiscFlags = 0;
					desc.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
					unsigned int offset = 0;
					imageData.pSysMem = rawData + offset;
					imageData.SysMemPitch = 4 * 1024;
					imageData.SysMemSlicePitch = 0;

					HRESULT r = device->CreateTexture2D(&desc, &imageData, &m_texture);
					if (r!=S_OK) { m_texture = NULL; }
					break;
			}
			case REG:
				{
					D3D11_TEXTURE2D_DESC desc;
					desc.Width = 1024;
					desc.Height = 1024;
					desc.MipLevels = 1;
					desc.ArraySize = 1;
					desc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
					desc.SampleDesc.Count = 1;
					desc.SampleDesc.Quality = 0;
					desc.Usage = D3D11_USAGE_DEFAULT;
					desc.CPUAccessFlags = 0;
					desc.MiscFlags = 0;
					desc.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
					unsigned int offset = 4194304;
					imageData.pSysMem = rawData + offset;
					imageData.SysMemPitch = 4 * 1024;
					imageData.SysMemSlicePitch = 0;

					HRESULT r = device->CreateTexture2D(&desc, &imageData, &m_texture);
					if (r!=S_OK) { m_texture = NULL; }
					break;
			}
			case UPGRADE:
				{
					D3D11_TEXTURE2D_DESC desc;
					desc.Width = 1024;
					desc.Height = 1024;
					desc.MipLevels = 1;
					desc.ArraySize = 1;
					desc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
					desc.SampleDesc.Count = 1;
					desc.SampleDesc.Quality = 0;
					desc.Usage = D3D11_USAGE_DEFAULT;
					desc.CPUAccessFlags = 0;
					desc.MiscFlags = 0;
					desc.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
					unsigned int offset = 8388608;
					imageData.pSysMem = rawData + offset;
					imageData.SysMemPitch = 4 * 1024;
					imageData.SysMemSlicePitch = 0;

					HRESULT r = device->CreateTexture2D(&desc, &imageData, &m_texture);
					if (r!=S_OK) { m_texture = NULL; }
					break;
			}
			case ID_UNKNOWN:
			default:
				{
					D3D11_TEXTURE2D_DESC desc;
					desc.Width = 1024;
					desc.Height = 1024;
					desc.MipLevels = 1;
					desc.ArraySize = 1;
					desc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
					desc.SampleDesc.Count = 1;
					desc.SampleDesc.Quality = 0;
					desc.Usage = D3D11_USAGE_DEFAULT;
					desc.CPUAccessFlags = 0;
					desc.MiscFlags = 0;
					desc.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
					unsigned int offset = 12582912;
					imageData.pSysMem = rawData + offset;
					imageData.SysMemPitch = 4 * 1024;
					imageData.SysMemSlicePitch = 0;

					HRESULT r = device->CreateTexture2D(&desc, &imageData, &m_texture);
					if (r!=S_OK) { m_texture = NULL; }
					break;
			}
		}
	};
	~Prompt()
	{
		if (m_texture!=NULL)
		{
			m_texture->Release();
		}
	}
	void Release()
	{
		if (m_texture!=NULL)
		{
			m_texture->Release();
			m_texture = NULL;
		}
	}
	ID3D11Texture2D* GetTexture() { return m_texture; }
};
