# Quick Use Examples
This section covers some quick examples to get you started.

Access Services via the `Services` static class.

## Binding and Aliasing
```cs
public class Foo : MonoBehaviour
{
  private void Awake()
  {
    // binds this Foo instance to the global context. MonoBehaviours are added to DDOL.
    var aliaser = Services.Bind<Foo>(this);
    aliaser.As<MonoBehaviour>(); // binds the registered Foo instance as a MonoBehaviour to the global scope.

    // binds this Foo instance to the MonoBehaviour's gameobject as Foo, MonoBehaviour, and object.
    Services.Bind<Foo>(this, gameObject).As<MonoBehaviour>().As<object>();

    // all Services.Bind calls:
    Services.Bind<Foo>(this); // globally. GameObjects are added to DDOL.
    Services.Bind<Foo>(this, gameObject.scene); // to the scene.
    Services.Bind<Foo>(this, gameObject); // to this MonoBehaviour's gameObject.
  }
}
```
## Getting and Resolving
```cs
public class Foo : MonoBehaviour
{
  private void Start()
  {
    // gets the scoped resolver for the current instance of Foo, which is a MonoBehaviour.
    // passing 'this' as a parameter only works if 'this' is a Component.
    // Services.For calls return IScopedResolver instances.
    var resolver = Services.For(this);

    var bar = resolver.Get<Bar>(); // gets a service of type Bar, throwing an exception if it does not exist.
    if(resolver.TryGet(out Bar bar)) ; // tries to get a service of type Bar safely, returning false if it could not.

    // check if the resolver is still in scope. this should be called if you are storing the resolver instance and
    // are unsure if it is still in scope or not while using the 'Get<T>()' method. the 'TryGet<T>()' method will
    // return false when not in scope while the 'Get<T>()' method will throw an exception.
    if(resolver.IsInScope()) ;
  }
}
```
> [!IMPORTANT]  
> Scoped Resolvers will always fall back to a higher scope when no service is found, e.g. when calling
> Services.For(Component).Get\<Bar\>(), a service may be returned from the game object, one of the game object's parents,
> the scene that game object exists in, or the global context.

> [!TIP]
> When calling Services.For(Component) or Services.For(GameObject), there is an optional second parameter to determine if the resolver
> should search for services using the game object's hierarchy or not. By default it will.

## Advanced Usage
For advanced usage, such as registering providers, transient services, and 'managed' services, you can access the service locator directly:
```cs
public FooBar()
{
  var locator = Services.GetLocator();
}
```
Registered providers and transient services will be retrieved by Scoped Resolvers, but managed services will need to be retrieved manually.

# How to Install
This section covers some ways to install this unity package.

The minimum version this package is tested with is Unity 6.0, but I believe it should work for earlier versions
as long as it has C# version 9 support. Breaking compatibility with previous versions of unity will result in
a tag for that version being pushed.

## Using Git with UPM

> [!NOTE]
> This requires git installed and added to the PATH environment variable.

In the unity package manager window, click the + in the top right corner, then select the 'add git package' option.
Then, add one of the following URLs:

### Releases
**HTTPS**
```
https://github.com/Heroshrine/SystemScrap.ServiceLocator.git#v1.1.2
```

**SSH**
```
git@github.com:Heroshrine/SystemScrap.ServiceLocator.git#v1.1.2
```
> [!TIP]
> You can replace the version code with any [release version](https://github.com/Heroshrine/SystemScrap.ServiceLocator/releases).

### Update Stream
**HTTPS**
```
https://github.com/Heroshrine/SystemScrap.ServiceLocator.git
```

**SSH**
```
git@github.com:Heroshrine/SystemScrap.ServiceLocator.git
```

## Adding as a Custom Package
- Go to the [releases](https://github.com/Heroshrine/SystemScrap.ServiceLocator/releases) page
- Download the source code for one of the releases
- In your Unity project's root folder, navigate to the Packages folder
- Copy the folder from inside the downloaded zip file to the Packages folder

## Forking and Adding as a Git Submodule

> [!NOTE]
> This assumes your project is set up as a git repository.

> [!IMPORTANT]
> If you plan to submit a pull request, this repository requires signed commits. Set up GPG signing.

When you add a repository as a submodule, it acts as a repository inside of a repository, where individual changes to the submodule aren't track in the 'parent' repository.
Some git applications may handle submodules (such as Rider's built-in git tools), some may not. Changes in submodules need to be committed and pushed separately from the
'parent' repository. To do so, use the command line to navigate to the submodule and directly commit and push your files using `git add .`, `git commit -m "message"`, and `git push`.
I'm not a git wizard, so if you want to learn more about submodules [check here](https://git-scm.com/book/en/v2/Git-Tools-Submodules).

### Adding the Submodule
First, fork this repository ([Documentation](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/working-with-forks/fork-a-repo)) or download and re-host the source code.

Next, open your repository's root folder using a terminal such as git bash.

Then, add the submodule. Make sure to replace the url with your forked repo's url.
```
git submodule add https://github.com/User/ServiceLocator.git Packages/com.systemscrap.servicelocator
```

> [!IMPORTANT]
> When _cloning_ a repository using submodules, you need to either add the `--recurse-submodules` argument to your clone command or you need to run two additional commands to also pull the submodules:
> ```
> git submodule init
> git submodule update
> ```

> [!TIP]
> Certain git operations may cause submodules to enter a detached head state despite their head seemingly not being detached. To fix this, you can run this command:
> ```
> git submodule foreach --recursive git checkout main
> ```

# The Registered Services Window
The Registered Services window is an editor tool that can be accessed from the toolbar at `Window > Registered Services`. This window shows you where all your services live at,
helping you understand what objects have access to what services. This helps you avoid and debug potential issues relating to service registration and resolution.

The Registered Services window heavily relies on reflection, so it is not performant. It is editor-only, so it will not affect runtime performance.

# AI Usage
AI was strictly used in the following capacity:
- To help create unit tests
- To help create editor tools
- As a code-reviewing tool
- As a documentation-writing tool

**None of the code that is compiled into your game was generated using AI.** Using this package you can still ethically claim your project did not use generative AI for any content included in your game.
