# My2048
A clone of the puzzle game 2048 created with Unity and Photoshop for Android

Download it on [Google Play](https://play.google.com/store/apps/details?id=com.SunsetWorkshop.My2048)

![alt tag](https://i.imgur.com/5JclVwwm.png)

My version is essentially faithful to the original game except for the undo button. You can only undo your last move once. If you ask me I think having undo makes the game too easy :)

## Optimizations

Sprites are batched together to reduce draw calls. All gameobjects are instantiated once and are pooled and reused to maintain constant memory usage and reduce Garbage Collector lag spikes

## TODO

Since this project is a bit old, I could optimize the logic for handling the movement + combination of matching tiles. Now knowing how the game works fundamentally I could rewrite it with less code and complexity

## Lessons Learned

Originally published on Google Play in 2018. Wrapping my head around the idea of sliding tiles around and having numbers combine only if they are equal was quite challenging at first. I learned a lot about breaking steps down into simple sequential transactions, which was necessary and extremely useful when implementing the undo feature
