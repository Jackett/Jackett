import { Note } from "@geist-ui/core";

export default function Error({ children }: React.PropsWithChildren<{}>) {
	return (
		<Note type="error" label="error" mb={2}>
			{children}
		</Note>
	);
}
