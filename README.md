# DotsAnimation

This is a Unity DOTS animation library that can be used like Unreal animation.

### define

- Animation Graph: You can save all the animation variables, and it supports to use of nodes to control the Skeleton bones, and define the final animation pose for a Skeletal Mesh to use per frame.
- Animation State Machine: Provide a way to break animation of a Skeletal Mesh into a series of States. These states are then governed by Transition Rules that control how to blend from one state to another.
- Animation State: It is a portion of an animation graph that we know the character will be blending into and out of on a regular basis. You can use different sample nodes and blend nodes to define the final animation pose for each Animation State.
- Animation Transition: After you defined your states, you need to use Animation Transition to control how your character is going to transition from one state to another, including transition time, transition rules, how to blend, etc.

### example

You can find some examples in SampleScene.scene. And the sample graph is Assets/Resources/Graph/AnimationGraphTest.asset

![image](https://user-images.githubusercontent.com/32125402/210300911-879d1365-a582-49a6-8896-d9a734885b19.png)

![image](https://user-images.githubusercontent.com/32125402/210302540-8c05c8ca-3e4c-4da9-a066-5b67ce1471c1.png)

![image](https://user-images.githubusercontent.com/32125402/210302563-760c779a-9b8c-4199-bd4c-e24e56b09a84.png)

### Nodes

- Get variable
- Single clip
- State machine
- Entry State
- Transition
- FinalPose
- IK todo
- BlendSpace todo
- Blend based on layer todo

### Flow
1. AnimationGraphBlobberSystem: to convert unity gameobject to dots entity
2. AnimationSetParameterSystem: will set all the animation parameter based on event
3. AnimationGraphSystem
    - UpdateAnimationGraphNodeJob update animation nodes which in the graph
    - set animation state machine weight based on animation node result
5. AnimationStateMachineSystem: init state machine and evaluate transition, then create new state if needed
6. AnimationChangeStateSystem: handle change state event and blend pose during state transition
7. UpdateAnimationNodesSystem: update all the animation states
8. AnimationBlendWeightsSystem: calc all animations blend weight
9. ClipSamplingSystem
    - sample optimize skeleton
    - raise animation events
    - send animation event to other system
    - sample root delta
    - apply root motion to entity
