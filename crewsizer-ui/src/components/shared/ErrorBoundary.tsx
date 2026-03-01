import { Component, type ErrorInfo, type ReactNode } from 'react';
import { AlertCircle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('[ErrorBoundary]', error, info.componentStack);
  }

  handleReload = () => {
    this.setState({ hasError: false, error: null });
  };

  render() {
    if (!this.state.hasError) {
      return this.props.children;
    }

    return (
      <div className="flex min-h-[50vh] items-center justify-center p-8">
        <Card className="max-w-lg">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-destructive">
              <AlertCircle className="h-5 w-5" />
              Une erreur est survenue
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-sm text-muted-foreground">
              L'application a rencontre une erreur inattendue. Vous pouvez
              essayer de recharger le composant ou la page.
            </p>
            {this.state.error && (
              <pre className="max-h-32 overflow-auto rounded-md bg-muted p-3 font-mono text-xs">
                {this.state.error.message}
              </pre>
            )}
            <div className="flex gap-2">
              <Button onClick={this.handleReload} variant="outline" size="sm">
                <RefreshCw className="mr-2 h-3.5 w-3.5" />
                Reessayer
              </Button>
              <Button onClick={() => window.location.reload()} size="sm">
                Recharger la page
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }
}
