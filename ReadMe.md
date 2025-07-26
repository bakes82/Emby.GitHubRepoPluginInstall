# Github Repo Plugin Installer
This plugin allows you to install plugins from GitHub repositories. It pulls the latest release from the repository and installs it as long as there is a .DLL file in the assets. It also supports automatic updates for the installed plugins.

## Configuration
You first need to create a Github PAT (Personal Access Token).
1. 🔑 Log in to GitHub:
   Go to GitHub and log in to your account.
2. ⚙️ Access Developer Settings:
   In the upper-right corner of any GitHub page, click your profile picture.
   Select Settings from the dropdown.
   Scroll down and, on the left sidebar, click Developer settings.
3. 🛠 Generate a Personal Access Token:
   In Developer settings, select Personal access tokens from the sidebar.
   Click on Tokens (classic) and then click Generate new token.
   
   **Important Scopes to Select:**
   - ✅ **repo** - Required for accessing private repositories (includes all repo permissions)
   - ✅ **read:org** - Required if accessing private repos in organizations
   
   For public repositories only, you can use a token with no scopes, but it's recommended to at least have basic read access.

Add the URL of the repository you want to install the plugin from. The URL should be in the format `https://github.com/owner/repository`.
Set option to auto update (optional). This will check for updates every 24 hours by default, you can adjust the schedule task.
Set option to allow pre-release versions (optional).

Optionally, you can set the plugin to auto restart Emby after installation of updated plugins, otherwise it will just put the notification on the dashboard.

You can also install the plugin manually after adding the repository. (It won't auto restart Emby after installation)

![PluginMainPage.png](imgs/PluginMainPage.png)

![PluginAddRepo.png](imgs/PluginAddRepo.png)

![PluginActivity.png](imgs/PluginActivity.png)