		<form action="/" method="POST" id="user_login"
		name="user_login" enctype="application/x-www-form-urlencoded">

			<label for="username">Benutzername: </label>
			<br />
			<input type="input" name="username" id="username" class="input_text" width="100%" />

			<br />
			<br />

			<label for="password">Password: </label>
			<br />
			<input type="password" name="password" id="password" class="input_text" width="100%" />
			<br />
			<br />
			<input type="submit" value="Einloggen" onclick="return sendForm('?provider=user&action=request&target=login','.##Ident##','.##Ident##','#userbox','login', '', '')" />
			
		</form>

		[[navigation(Target:user)~ffffffff-ffff-ffff-ffff-ffffffffffff]]
		
