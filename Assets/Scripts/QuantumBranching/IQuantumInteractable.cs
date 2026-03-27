namespace QuantumBranching
{
    public interface IQuantumInteractable
    {
        bool CanInteract(SimplePlayerInteractor interactor);
        string GetInteractionPrompt(SimplePlayerInteractor interactor);
        void Interact(SimplePlayerInteractor interactor);
    }
}
