;; invoke: f32const
;; expect: (f32:1.23)

(module
    (func (export "f32const") (result f32)
        (f32.const 1.23)
    )
)