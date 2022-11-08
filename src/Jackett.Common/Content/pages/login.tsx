import { Button, Card, Grid, Input, Text } from "@geist-ui/core";

export default function Login() {
	return (
		<Grid.Container justify="center" alignItems="center" height="100vh">
			<Grid xs={20} sm={20} md={8} lg={6}>
				<Card width="100%" height="100%">
					<form method="post">
						<Text className="center" h1>
							Jackett
						</Text>
						<Input.Password
							placeholder="Password"
							name="password"
							width="100%"
							autoFocus
							required
						/>
						<Button htmlType="submit" className="block" mt={1}>
							Login
						</Button>
					</form>
				</Card>
			</Grid>
		</Grid.Container>
	);
}
