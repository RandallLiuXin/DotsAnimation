# DotsAnimation

This is a Unity DOTS animation solution. Though still in progress, it is already capable for project usages. (Especially for now, unity doesn't have any official animation solution with the latest DOTS package.)

- Animation Graph: You can save all the animation variables on it. It supports using nodes to control the Skeleton bones and to define the character's final animation pose.
- Animation State Machine: Provide a way to break animation of a character into a series of States. These states are then governed by Transition Rules that control how to blend from one state to another.
- Animation State: It is a portion of an animation graph that we know the character will be blending into and out of on a regular basis. You can use different sample nodes and blend nodes to define the final animation pose for each Animation State.
- Animation Transition: After you defined your states, you need to use Animation Transition to control how your character is going to transit from one state to another, including transition time, transition rules, how to blend, etc.
- Animation Events
- Root motion
- Animation Compression: based on [ACL](https://github.com/nfrechette/acl)

### Example

You can find a sample graph here: Assets/Resources/Graph/AnimationGraphTest.asset

![image](https://user-images.githubusercontent.com/32125402/210300911-879d1365-a582-49a6-8896-d9a734885b19.png)

![image](https://user-images.githubusercontent.com/32125402/210302540-8c05c8ca-3e4c-4da9-a066-5b67ce1471c1.png)

![image](https://user-images.githubusercontent.com/32125402/210302563-760c779a-9b8c-4199-bd4c-e24e56b09a84.png)

You can refer to SampleScene.scene for a test example.

### How to test
1. Open SampleScene.scene and press play button
2. You can see three character. The left one is driven by unity origin animator. The middle one is my unity dots single clip test. The right one is unity dots animator

![image](https://user-images.githubusercontent.com/32125402/210693995-50f4220a-7284-46af-b386-fa2c7329a7d7.png)

3. Open DOTS hierarchy, and select RPG-Character_ecs_animationGraph

![image](https://user-images.githubusercontent.com/32125402/210693921-70881626-2c24-4d3a-9fb6-5d69547d59c3.png)

![image](https://user-images.githubusercontent.com/32125402/210693803-a01e9d63-5e83-4246-b630-5d977eadde21.png)

4. In the Inspector windows, you can find Bool Parameter

![image](https://user-images.githubusercontent.com/32125402/210693757-3a288262-fb69-4f01-b690-3317acc49dbf.png)

5. Set Moving value to true, you will see the character start to run. Set Dead value to true, you will see the character start to play stun animation, after finish playing stun animation, the character will play knockdown

![image](https://user-images.githubusercontent.com/32125402/210694282-9c095635-2348-4b5c-8192-9e8fa79b6764.png)

6. Looks like this

![image](https://user-images.githubusercontent.com/32125402/210694453-f2805ad4-6359-4da4-96e5-6394c83f58b4.png)

### How It Works
1. AnimationGraphBlobberSystem: convert unity gameobject to dots entity
2. AnimationSetParameterSystem: set all the animation parameter based on event
3. AnimationGraphSystem
    - UpdateAnimationGraphNodeJob update animation nodes which in the graph
    - set animation state machine weight based on animation node result
5. AnimationStateMachineSystem: init state machine and evaluate transition, then create new state if needed
6. AnimationChangeStateSystem: handle change state event and blend pose during state transition
7. UpdateAnimationNodesSystem: update all the animation states
8. AnimationBlendWeightsSystem: calculate all animations blend weight
9. ClipSamplingSystem
    - sample optimize skeleton
    - raise animation events
    - send animation event to other system
    - sample root delta
    - apply root motion to entity

### Supporting Nodes for now
- Get variable
- Single clip
- State machine
- Entry State
- Transition
- FinalPose

### Todo
- BlendSpace
- Blend based on layer
- LOD
- IK 
