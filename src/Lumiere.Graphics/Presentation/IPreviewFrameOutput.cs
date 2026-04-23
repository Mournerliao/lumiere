using Vortice.Direct3D11;

namespace Lumiere.Graphics.Presentation;

internal interface IPreviewFrameOutput
{
    void CopyFrame(ID3D11Texture2D? texture);

    void Present();
}
