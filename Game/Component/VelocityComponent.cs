using System;
using Godot;
using GodotUtilities;
using SampleGodotCSharpProject.Game.Autoload;
using Vector2 = Godot.Vector2;

namespace SampleGodotCSharpProject.Game.Component;

public partial class VelocityComponent : BaseComponent
{
    [Export]
    public Vector2 Velocity = Vector2.Zero;

    [Export]
    public Vector2 Gravity = new Vector2(0f, 0f);

    [Export]
    public PhysicsBody2D ContinuousProcess;

    [Export]
    public CollisionShape2D CollisionShape2D;

    [Node]
    public Label Label;

    public bool Falling { get; private set; }

    private float _speed;

    public float Speed
    {
        get => _speed;

        private set => _speed = value;
    }

    [Signal]
    public delegate void CollidedEventHandler(KinematicCollision2D collision2D);

    private Vector2 _lastPosition = Vector2.Zero;

    public override void _EnterTree()
    {
        this.WireNodes();
    }

    public override void _Ready()
    {
        SetPhysicsProcess(false);
    }

    private void _UpdateSpeed()
    {
        Speed = (_lastPosition - GlobalPosition).LengthSquared();
        _lastPosition = GlobalPosition;
        if (Label.Visible)
            Label.Text = $"{Falling} - {CollisionShape2D?.Disabled} - {Speed}/s";
    }

    private KinematicCollision2D _CalculateSpeed(Func<KinematicCollision2D> action)
    {
        var result = action.Invoke();
        _UpdateSpeed();
        return result;
    }

    private bool _CalculateSpeed(Func<bool> action)
    {
        var result = action.Invoke();
        _UpdateSpeed();
        return result;
    }

    public override void _PhysicsProcess(double delta)
    {
        MoveAndCollide(ContinuousProcess, delta);
        Falling = Gravity.LengthSquared() > 0.0f;
    }

    public void DisableCollisionCheck(bool flag)
    {
        CollisionShape2D?.CallDeferred("set_disabled", flag);
    }

    public KinematicCollision2D MoveAndCollide(PhysicsBody2D node, double delta)
    {
        Velocity += Gravity;
        
        var collision2D = _CalculateSpeed(() => node.MoveAndCollide(Velocity * (float)delta));
        if (collision2D == null) return null;
        
        _EmitCollision(collision2D);
        
        return collision2D;
    }

    public void MoveAndSlide(CharacterBody2D node)
    {
        Velocity += Gravity;
        node.Velocity = Velocity;
        var collided = _CalculateSpeed(node.MoveAndSlide);
        if (!collided) return;
        
        var collision2D = node.GetLastSlideCollision();
        _EmitCollision(collision2D);
    }

    private void _EmitCollision(KinematicCollision2D collision2D)
    {
        EmitSignal(SignalName.Collided, collision2D);
        GameEvents.EmitCollision(collision2D);
    }

    public void EnablePhysics(bool flag)
    {
        SetPhysicsProcess(flag);
        if (flag == false)
        {
            Falling = false;
        }
    }

    public void ApplyGravity(Node2D node, Vector2 gravity)
    {
        Velocity = Vector2.Zero;
        Gravity = gravity;
        ContinuousProcess = node as PhysicsBody2D;
        SetPhysicsProcess(true);
    }
}