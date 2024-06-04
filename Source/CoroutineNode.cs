using Godot;

namespace Til.GodotCoroutine;

public partial class CoroutineNode : Node {
    
    
    
    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);
    }

    public override void _Process(double delta) {
        base._Process(delta);
    }

    public override void _ExitTree() {
        base._ExitTree();
        
    }
}

public class CoroutineContext {
    
    
}