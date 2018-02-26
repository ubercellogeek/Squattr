# Squattr
An example of how to list and reserve your O365 organizations conference rooms all from Slack


1. Setup a user within your O365 organization with read rights to the shared calendars of your conference room objects.
2. Setup a Slash command in your team and point it at a deployed API in this solution.
3. Setup slash command webhooks to appropriately hit your API endpoints. In the end, you should be able to do the following within you team's channels, for example:

`/squattr status` - Get a list of all conference rooms and their immediate reservation status<br>
`/squattr commerce` - Get the current reservation status of the `commerce` conference room.