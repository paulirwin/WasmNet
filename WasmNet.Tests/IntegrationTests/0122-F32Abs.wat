;; invoke: abs
;; expect: (f32:42)

(module
    (func (export "abs") (result f32)
        f32.const -42.0
        f32.abs
    )
)